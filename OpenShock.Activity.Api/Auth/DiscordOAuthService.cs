using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OpenShock.Activity.Api.Config;

namespace OpenShock.Activity.Api.Auth;

public sealed record DiscordIdentity(ulong Id, string Name, string AccessToken, string? Avatar, bool WidgetAuthorized);

public sealed class DiscordOAuthService
{
    private const string TokenUrl = "https://discord.com/api/oauth2/token";
    private const string MeUrl = "https://discord.com/api/users/@me";

    /// <summary>OAuth2 scope that lets the bot write the user's profile widget (Game Stats Widget) data.</summary>
    public const string WidgetScope = "application_identities.write";

    private readonly HttpClient _http;
    private readonly DiscordConfig _cfg;
    private readonly ILogger<DiscordOAuthService> _logger;

    public DiscordOAuthService(HttpClient http, ActivityApiConfig cfg, ILogger<DiscordOAuthService> logger)
    {
        _http = http;
        _cfg = cfg.Discord;
        _logger = logger;
    }

    /// <summary>
    /// Exchanges an OAuth authorization code (from the Embedded App SDK) for a Discord access token,
    /// then fetches the authenticated user's identity. Returns null on any failure.
    /// </summary>
    public async Task<DiscordIdentity?> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        DiscordTokenResponse? token;
        using (var tokenResponse = await _http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
               {
                   ["client_id"] = _cfg.ClientId,
                   ["client_secret"] = _cfg.ClientSecret,
                   ["grant_type"] = "authorization_code",
                   ["code"] = code
               }), ct))
        {
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Discord token exchange failed with status {Status}", tokenResponse.StatusCode);
                return null;
            }

            token = await tokenResponse.Content.ReadFromJsonAsync<DiscordTokenResponse>(ct);
        }

        if (token?.AccessToken is null) return null;

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, MeUrl);
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var meResponse = await _http.SendAsync(meRequest, ct);
        if (!meResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Discord /users/@me failed with status {Status}", meResponse.StatusCode);
            return null;
        }

        var me = await meResponse.Content.ReadFromJsonAsync<DiscordMeResponse>(ct);
        if (me is null || !ulong.TryParse(me.Id, out var id)) return null;

        var widgetAuthorized = token.Scope?.Split(' ').Contains(WidgetScope) ?? false;
        return new DiscordIdentity(id, me.GlobalName ?? me.Username, token.AccessToken, me.Avatar, widgetAuthorized);
    }

    private sealed record DiscordTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("scope")] string? Scope);

    private sealed record DiscordMeResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("global_name")] string? GlobalName,
        [property: JsonPropertyName("avatar")] string? Avatar);
}
