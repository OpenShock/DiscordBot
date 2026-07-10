using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Problems;

namespace OpenShock.Activity.Api.Controllers;

[AllowAnonymous]
public sealed class AuthController : ActivityControllerBase
{
    private readonly DiscordOAuthService _discord;
    private readonly JwtTokenService _jwt;
    private readonly IHostEnvironment _env;

    public AuthController(DiscordOAuthService discord, JwtTokenService jwt, IHostEnvironment env)
    {
        _discord = discord;
        _jwt = jwt;
        _env = env;
    }

    /// <summary>Exchange a Discord Embedded App SDK authorization code for our session JWT.</summary>
    [HttpPost("auth/token")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Token(TokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code)) return Problem(AuthError.MissingCode);

        var identity = await _discord.ExchangeCodeAsync(req.Code, ct);
        if (identity is null) return Problem(AuthError.DiscordExchangeFailed);

        var token = _jwt.Issue(identity.Id, identity.Name);
        return Ok(new TokenResponse(identity.AccessToken, token,
            new AuthUserDto(identity.Id.ToString(), identity.Name, identity.Avatar)));
    }

    /// <summary>Development-only shortcut to mint a JWT without going through Discord (local API testing).</summary>
    [HttpPost("dev/token")]
    [ProducesResponseType<DevTokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DevToken(DevTokenRequest req)
    {
        if (!_env.IsDevelopment()) return NotFound();

        var name = string.IsNullOrWhiteSpace(req.Name) ? $"Dev {req.DiscordId}" : req.Name;
        return Ok(new DevTokenResponse(_jwt.Issue(req.DiscordId, name)));
    }
}
