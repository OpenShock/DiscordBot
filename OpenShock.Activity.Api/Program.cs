using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenShock.DiscordBot;
using OpenShock.Activity.Api;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Config;
using OpenShock.Activity.Api.Endpoints;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

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

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.Converters.Add(new UInt64StringConverter());
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

// In production the Discord proxy makes every call same-origin, so CORS is only needed for local dev.
if (app.Environment.IsDevelopment())
    app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Endpoints are served at ROOT: Discord strips the "/api" URL-mapping prefix before requests reach us.
app.MapAuthEndpoints(app.Environment);

var api = app.MapGroup("").RequireAuthorization();
api.MapUserEndpoints();
api.MapShockerEndpoints();
api.MapWhitelistEndpoints();
api.MapControlEndpoints();

app.MapHub<RoomHub>("/hubs/room");

app.Run();

public partial class Program;
