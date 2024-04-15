namespace OpenShock.DiscordBot;

public sealed class DiscordBotConfig
{
    public required string Token { get; init; }
    public required string Db { get; init; }
}