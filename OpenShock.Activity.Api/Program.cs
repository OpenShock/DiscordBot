using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenShock.Activity.Api;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Config;
using OpenShock.Activity.Api.Endpoints;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

var config = builder.Configuration.GetSection("Activity").Get<ActivityApiConfig>()
             ?? throw new InvalidOperationException("Could not load the 'Activity' configuration section.");
builder.Services.AddSingleton(config);

builder.Services.AddDbContext<OpenShockDiscordContext>(options =>
{
    options.UseNpgsql(config.Db.Conn);
    if (config.Db.Debug)
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddScoped<IOpenShockBackendService, OpenShockBackendService>();
builder.Services.AddSingleton<RoomRegistry>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<DiscordOAuthService>();
builder.Services.AddSignalR().AddJsonProtocol(o =>
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
