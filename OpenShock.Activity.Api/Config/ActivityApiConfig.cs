using System.ComponentModel.DataAnnotations;
using OpenShock.DiscordBot;

namespace OpenShock.Activity.Api.Config;

public sealed class ActivityApiConfig
{
    public required DiscordConfig Discord { get; init; }
    public required JwtConfig Jwt { get; init; }
    public required DbConfig Db { get; init; }

    /// <summary>Origins allowed by CORS. Only used for local development; in production the Discord proxy makes calls same-origin.</summary>
    public string[] CorsOrigins { get; init; } = [];
}

public sealed class DiscordConfig
{
    [Required(AllowEmptyStrings = false)] public required string ClientId { get; init; }
    [Required(AllowEmptyStrings = false)] public required string ClientSecret { get; init; }
}

public sealed class JwtConfig
{
    [Required(AllowEmptyStrings = false)] public required string Key { get; init; }
    public string Issuer { get; init; } = "openshock-activity";
    public string Audience { get; init; } = "openshock-activity";
    public int LifetimeDays { get; init; } = 7;
}
