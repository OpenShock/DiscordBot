using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace OpenShock.DiscordBot.Services.Widget;

public enum ProfileWidgetResult
{
    Success,

    /// <summary>The user no longer grants <c>application_identities.write</c>, stop writing until they do again.</summary>
    Unauthorized,

    /// <summary>Anything else (rate limits, outages), worth retrying later.</summary>
    Failed
}

/// <summary>
/// Writes Game Stats Widget data through Discord's Application Identity Profile API, authorized with the bot token.
/// </summary>
public sealed class ProfileWidgetClient
{
    private readonly HttpClient _http;
    private readonly DiscordBotConfig _config;
    private readonly ILogger<ProfileWidgetClient> _logger;

    public ProfileWidgetClient(HttpClient http, DiscordBotConfig config, ILogger<ProfileWidgetClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Replaces the user's widget data for the given OpenShock identity. Discord replaces the whole data object on
    /// every PATCH, so <paramref name="data"/> must always be complete; an empty object clears it.
    /// </summary>
    public async Task<ProfileWidgetResult> UpdateAsync(ulong applicationId, ulong discordId, Guid openShockId,
        JsonObject data, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch,
            $"https://discord.com/api/v10/applications/{applicationId}/users/{discordId}/identities/{openShockId}/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _config.Token);
        request.Content = JsonContent.Create(new JsonObject { ["data"] = data });

        using var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return ProfileWidgetResult.Success;

        _logger.LogWarning("Profile widget update for {DiscordId} failed with {Status}: {Error}",
            discordId, response.StatusCode, await response.Content.ReadAsStringAsync(ct));

        return response.StatusCode == System.Net.HttpStatusCode.Forbidden
            ? ProfileWidgetResult.Unauthorized
            : ProfileWidgetResult.Failed;
    }
}
