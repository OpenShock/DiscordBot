using OpenShock.Activity.Api.Auth;

namespace OpenShock.Activity.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment env)
    {
        // Exchange a Discord Embedded App SDK authorization code for our session JWT.
        app.MapPost("/auth/token",
            async (TokenRequest req, DiscordOAuthService discord, JwtTokenService jwt, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(req.Code)) return Results.BadRequest(new { error = "Missing code" });

                var identity = await discord.ExchangeCodeAsync(req.Code, ct);
                if (identity is null) return Results.Unauthorized();

                var token = jwt.Issue(identity.Id, identity.Name);
                return Results.Ok(new TokenResponse(identity.AccessToken, token,
                    new AuthUserDto(identity.Id.ToString(), identity.Name, identity.Avatar)));
            }).AllowAnonymous();

        // Development-only shortcut to mint a JWT without going through Discord, for local API testing.
        if (env.IsDevelopment())
        {
            app.MapPost("/dev/token", (DevTokenRequest req, JwtTokenService jwt) =>
            {
                var name = string.IsNullOrWhiteSpace(req.Name) ? $"Dev {req.DiscordId}" : req.Name;
                var token = jwt.Issue(req.DiscordId, name);
                return Results.Ok(new { jwt = token });
            }).AllowAnonymous();
        }
    }
}
