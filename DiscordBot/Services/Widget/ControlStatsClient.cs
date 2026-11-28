using System.Net.Http.Json;

namespace OpenShock.DiscordBot.Services.Widget;

/// <summary>
/// Client for the OpenShock API's integration endpoint that reports users' lifetime control stats, but only
/// for users whose stats changed since a cursor.
/// </summary>
public sealed class ControlStatsClient
{
    public const string AutomationTokenHeader = "X-OpenShock-Automation-Token";

    /// <summary>Server side limit of user ids per request.</summary>
    private const int MaxUserIds = 1000;

    private readonly HttpClient _http;

    public ControlStatsClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Totals of every user in <paramref name="userIds"/> whose stats changed since <paramref name="since"/>, or of
    /// all of them when it is null. The returned cursor is the oldest one of all chunks.
    /// </summary>
    public async Task<ControlStatChanges> GetChangesAsync(DateTime? since, IReadOnlyCollection<Guid> userIds,
        CancellationToken ct)
    {
        DateTime? cursor = null;
        var users = new List<UserControlStatTotals>();

        foreach (var chunk in userIds.Chunk(MaxUserIds))
        {
            using var response = await _http.PostAsJsonAsync("1/integrations/control-stats/changes",
                new { since, userIds = chunk }, ct);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<ControlStatChanges>(ct)
                       ?? throw new InvalidOperationException("Empty control stats response");
            users.AddRange(page.Users);
            if (cursor is null || page.Cursor < cursor) cursor = page.Cursor;
        }

        return new ControlStatChanges(cursor ?? DateTime.UtcNow, users);
    }
}

public sealed record ControlStatChanges(DateTime Cursor, IReadOnlyList<UserControlStatTotals> Users);

public sealed record UserControlStatTotals(Guid UserId, ControlStatTotals[] Received, ControlStatTotals[] Given);

/// <summary>One control type's totals, <see cref="Type"/> is the API's enum name (Shock, Vibrate, Sound).</summary>
public sealed record ControlStatTotals(
    string Type,
    bool Live,
    long Count,
    long DurationMs,
    double AverageIntensity,
    byte MaxIntensity);
