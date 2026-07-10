using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenShock.DiscordBot;
using OpenShock.Activity.Api;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Config;
using OpenShock.Activity.Api.Problems;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using Serilog;
using StackExchange.Redis;

// Minimal logger until the configured Serilog pipeline is built, so startup failures are still logged.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // appsettings.Custom.json is mounted in production (see the gitops logging-config) and carries the
    // shared Serilog OpenTelemetry sink config; optional so local dev falls back to the console sink.
    builder.Configuration.AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false);
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

    // Serilog, configured entirely from the "Serilog" config section (same pattern as the bot and main API).
    builder.Host.UseSerilog((context, _, cfg) => cfg.ReadFrom.Configuration(context.Configuration));

    var config = builder.Configuration.GetSection("Activity").Get<ActivityApiConfig>()
                 ?? throw new InvalidOperationException("Could not load the 'Activity' configuration section.");
    // Shared top-level "Db" section (same section the Discord bot reads).
    var dbConfig = builder.Configuration.GetSection("Db").Get<DbConfig>()
                   ?? throw new InvalidOperationException("Could not load the 'Db' configuration section.");
    builder.Services.AddSingleton(config);
    builder.Services.AddSingleton(dbConfig);

    builder.Services.AddDbContext<OpenShockDiscordContext>(options =>
    {
        options.UseNpgsql(dbConfig.Conn);
        if (dbConfig.Debug)
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }
    });

    // Liveness + DB readiness. AddDbContextCheck runs a lightweight CanConnect against Postgres.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<OpenShockDiscordContext>("database");

    builder.Services.AddScoped<IOpenShockBackendService, OpenShockBackendService>();
    builder.Services.AddSingleton<JwtTokenService>();
    builder.Services.AddHttpClient<DiscordOAuthService>();

    // Redis is optional. When configured it enables horizontal scaling: a shared presence store plus a
    // SignalR backplane so group broadcasts reach clients on every replica. Without it we fall back to
    // single-instance in-memory presence.
    var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
    var signalR = builder.Services.AddSignalR();
    if (redisConfig is { IsConfigured: true })
    {
        var redisOptions = redisConfig.ToConfigurationOptions();
        var mux = ConnectionMultiplexer.Connect(redisOptions);
        builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
        builder.Services.AddSingleton<RedisRoomRegistry>();
        builder.Services.AddSingleton<IRoomRegistry>(sp => sp.GetRequiredService<RedisRoomRegistry>());
        builder.Services.AddHostedService<RoomPresenceMaintenanceService>();
        signalR.AddStackExchangeRedis(o =>
        {
            o.Configuration = redisOptions;
            o.Configuration.ChannelPrefix = RedisChannel.Literal("activity");
        });
    }
    else
    {
        builder.Services.AddSingleton<IRoomRegistry, InMemoryRoomRegistry>();
    }

    signalR.AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.PayloadSerializerOptions.Converters.Add(new UInt64StringConverter());
    });

    builder.Services.AddControllers().AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new UInt64StringConverter());
    });

    // RFC 7807 problem responses, like the main OpenShock API. Model-validation failures are shaped as our
    // ValidationProblem so every error the client sees is an application/problem+json OpenShockProblem.
    builder.Services.AddProblemDetails();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            new ValidationProblem(context.ModelState).ToObjectResult(context.HttpContext);
    });

    // OpenAPI document (served at /openapi/v1.json) that drives the frontend's hey-api client generation.
    builder.Services.AddOpenApi(options =>
    {
        options.AddSchemaTransformer<ActivitySchemaTransformer>();
        // Drop the request-derived server URL so the generated client is port-independent (the frontend
        // supplies the base URL at runtime); this keeps the committed client stable across dev ports.
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Servers?.Clear();
            return Task.CompletedTask;
        });
    });

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Jwt.Key));
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = config.Jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                NameClaimType = "name"
            };

            // SignalR's WebSocket transport can't send an Authorization header — it passes the JWT via query.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    const string corsPolicy = "activity";
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
    {
        if (config.CorsOrigins.Length > 0)
            policy.WithOrigins(config.CorsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }));

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // In production the Discord proxy makes every call same-origin, so CORS is only needed for local dev.
    if (app.Environment.IsDevelopment())
    {
        app.UseCors(corsPolicy);
        // OpenAPI is only needed for local client generation, so it's not exposed in production.
        app.MapOpenApi().AllowAnonymous();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthResponse }).AllowAnonymous();

    // Controllers are served at ROOT: Discord strips the "/api" URL-mapping prefix before requests reach us.
    app.MapControllers();

    app.MapHub<RoomHub>("/hubs/room");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Activity API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Writes the health report as JSON; the middleware still maps the HTTP status (200 healthy / 503 not).
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            error = e.Value.Exception?.Message
        })
    });
}

public partial class Program;
