using OpenShock.DiscordBot.Services;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.Activity.Api;

// ---- Auth ----
public sealed record TokenRequest(string Code);
public sealed record DevTokenRequest(ulong DiscordId, string? Name);
public sealed record TokenResponse(string DiscordAccessToken, string Jwt, AuthUserDto User);
public sealed record AuthUserDto(string DiscordId, string Name, string? Avatar);

// ---- Me / link / consent ----
public sealed record MeResponse(bool Linked, bool AllowRoomShocks, byte RoomMaxIntensity, int RoomMaxDurationMs);
public sealed record LinkRequest(string ApiToken, string ApiServer);
public sealed record LinkResponse(string OpenShockName);
public sealed record ConsentRequest(bool AllowRoomShocks, byte RoomMaxIntensity, int RoomMaxDurationMs);

// ---- Shockers ----
public sealed record ShockerDto(Guid Id, string Name, string HubName, bool Enabled);
public sealed record SetShockersRequest(Guid[] EnabledIds);

// ---- Whitelist ----
public sealed record WhitelistEntryDto(string DiscordId);

// ---- Control ----
public sealed record ControlRequestDto(
    ulong TargetDiscordId,
    string InstanceId,
    byte Intensity,
    float Duration,
    ControlType Type,
    ShockMode Mode);
