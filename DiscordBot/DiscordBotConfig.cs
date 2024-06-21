using System.ComponentModel.DataAnnotations;

namespace OpenShock.DiscordBot;

public sealed class DiscordBotConfig
{
    public required string Token { get; init; }
    public required DbConfig Db { get; init; }
}

public sealed class DbConfig
{
    [Required(AllowEmptyStrings = false)] public required string Conn { get; init; }
    public bool SkipMigration { get; init; } = false;
    public bool Debug { get; init; } = false;
}