namespace OpenShock.DiscordBot;

public sealed class DiscordBotConfig
{
    public required string Token { get; init; }

    public WidgetSyncConfig Widget { get; init; } = new();
}

/// <summary>Keeps users' Discord profile widgets in sync with their OpenShock control stats.</summary>
public sealed class WidgetSyncConfig
{
    /// <summary>The OpenShock API stats are read from. Users linked to any other server are skipped.</summary>
    public Uri ApiServer { get; init; } = new("https://api.openshock.app/");

    /// <summary>Automation token granting <c>ControlStatsRead</c> on <see cref="ApiServer"/>. Sync is off when unset.</summary>
    public string? AutomationToken { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);
}
