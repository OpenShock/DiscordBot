using System.Text.Json.Nodes;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Services.Widget;

/// <summary>
/// Periodically pushes the OpenShock control stats of users who granted <c>application_identities.write</c> to
/// their Discord profile widget. Each tick costs one stats request: the API only reports users whose stats changed
/// since the previous cursor, and users seen for the first time get one full sync. A widget is only written when
/// its data actually differs from what was last written.
/// </summary>
public sealed class WidgetSyncService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly WidgetSyncConfig _config;
    private readonly ILogger<WidgetSyncService> _logger;

    /// <summary>What was last written per Discord user, so unchanged data and stale identities can be detected.</summary>
    private readonly Dictionary<ulong, SyncedWidget> _synced = [];

    private DateTime? _cursor;

    public WidgetSyncService(IServiceProvider services, DiscordSocketClient client, DiscordBotConfig config,
        ILogger<WidgetSyncService> logger)
    {
        _services = services;
        _client = client;
        _config = config.Widget;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_config.AutomationToken))
        {
            _logger.LogInformation("No widget automation token configured, profile widget sync is disabled");
            return;
        }

        using var timer = new PeriodicTimer(_config.Interval);
        do
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(e, "Profile widget sync failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        // The bot user's id is the application id, unknown until the gateway logged in
        if (_client.CurrentUser is not { } bot) return;
        var applicationId = bot.Id;

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
        var stats = scope.ServiceProvider.GetRequiredService<ControlStatsClient>();
        var widgets = scope.ServiceProvider.GetRequiredService<ProfileWidgetClient>();

        var authorized = (await db.Users.Where(x => x.WidgetAuthorized)
                .Select(x => new { x.DiscordId, x.OpenshockId, x.ApiServer })
                .ToArrayAsync(ct))
            .Where(x => IsSyncedServer(x.ApiServer))
            .ToDictionary(x => x.DiscordId, x => x.OpenshockId);

        // Unlinked, revoked or re-linked to another OpenShock account: clear what we wrote for the old identity
        foreach (var (discordId, synced) in _synced.ToArray())
        {
            if (authorized.TryGetValue(discordId, out var openShockId) && openShockId == synced.OpenShockId) continue;

            _synced.Remove(discordId);
            await widgets.UpdateAsync(applicationId, discordId, synced.OpenShockId, new JsonObject(), ct);
        }

        var updates = new List<UserControlStatTotals>();
        DateTime? cursor = null;

        if (_cursor is { } since && _synced.Count > 0)
        {
            var changes = await stats.GetChangesAsync(since, _synced.Values.Select(x => x.OpenShockId).ToArray(), ct);
            updates.AddRange(changes.Users);
            cursor = changes.Cursor;
        }

        var fresh = authorized.Where(x => !_synced.ContainsKey(x.Key)).Select(x => x.Value).ToArray();
        if (fresh.Length > 0)
        {
            var initial = await stats.GetChangesAsync(null, fresh, ct);
            updates.AddRange(initial.Users);
            if (cursor is null || initial.Cursor < cursor) cursor = initial.Cursor;
        }

        var discordIds = authorized.ToDictionary(x => x.Value, x => x.Key);
        foreach (var totals in updates)
        {
            if (!discordIds.TryGetValue(totals.UserId, out var discordId)) continue;

            var data = BuildWidgetData(totals);
            var serialized = data.ToJsonString();
            if (_synced.TryGetValue(discordId, out var synced) && synced.Data == serialized) continue;

            switch (await widgets.UpdateAsync(applicationId, discordId, totals.UserId, data, ct))
            {
                case ProfileWidgetResult.Success:
                    _synced[discordId] = new SyncedWidget(totals.UserId, serialized);
                    break;
                case ProfileWidgetResult.Unauthorized:
                    _synced.Remove(discordId);
                    await db.Users.Where(x => x.DiscordId == discordId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.WidgetAuthorized, false), ct);
                    break;
                default:
                    // Forgotten users get a full sync next tick, which retries the write
                    _synced.Remove(discordId);
                    break;
            }
        }

        if (cursor is not null) _cursor = cursor;
    }

    private bool IsSyncedServer(string apiServer) =>
        Uri.TryCreate(apiServer, UriKind.Absolute, out var uri) &&
        Uri.Compare(uri, _config.ApiServer, UriComponents.SchemeAndServer, UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>
    /// The widget's dynamic fields. Their names are the "User Data" keys the widget layout maps in the Developer
    /// Portal, so renaming one needs the layout updated too.
    /// </summary>
    private static JsonObject BuildWidgetData(UserControlStatTotals totals)
    {
        var fields = new JsonArray();

        foreach (var (direction, rows) in new[] { ("received", totals.Received), ("given", totals.Given) })
        {
            var shocks = rows.Where(x => x.Type == "Shock").ToArray();

            AddNumber(fields, $"shocks_{direction}", shocks.Sum(x => x.Count));
            AddNumber(fields, $"vibrations_{direction}", rows.Where(x => x.Type == "Vibrate").Sum(x => x.Count));
            AddNumber(fields, $"sounds_{direction}", rows.Where(x => x.Type == "Sound").Sum(x => x.Count));
            AddNumber(fields, $"shock_seconds_{direction}", Math.Round(shocks.Sum(x => x.DurationMs) / 1000.0, 1));
            AddNumber(fields, $"max_shock_intensity_{direction}", shocks.Select(x => (int)x.MaxIntensity).DefaultIfEmpty().Max());
        }

        return new JsonObject { ["dynamic"] = fields };
    }

    private static void AddNumber(JsonArray fields, string name, JsonNode value) =>
        fields.Add(new JsonObject { ["type"] = 2, ["name"] = name, ["value"] = value });

    private sealed record SyncedWidget(Guid OpenShockId, string Data);
}
