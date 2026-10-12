using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenShock.Activity.Api.Config;

namespace OpenShock.Activity.Api.Auth;

public sealed class JwtTokenService
{
    private readonly JwtConfig _cfg;

    public JwtTokenService(ActivityApiConfig cfg)
    {
        _cfg = cfg.Jwt;
        SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.Key));
    }

    public SymmetricSecurityKey SigningKey { get; }

    public string Issue(ulong discordId, string name, bool widgetAuthorized = false)
    {
        List<Claim> claims =
        [
            new("sub", discordId.ToString()),
            new("name", name)
        ];
        if (widgetAuthorized) claims.Add(new Claim(ClaimsExtensions.WidgetClaim, "true"));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _cfg.Issuer,
            Audience = _cfg.Audience,
            Expires = DateTime.UtcNow.AddDays(_cfg.LifetimeDays),
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
