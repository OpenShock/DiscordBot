using System.Net;

namespace OpenShock.Activity.Api.Problems;

// The Activity API's error catalog, grouped by domain like the main OpenShock API's error classes
// (e.g. HubError). Each entry is a typed OpenShockProblem the controllers return via Problem(...).

public static class AuthError
{
    public static OpenShockProblem MissingCode =>
        new("Auth.MissingCode", "Missing code", HttpStatusCode.BadRequest, "Missing code");

    public static OpenShockProblem DiscordExchangeFailed =>
        new("Auth.DiscordExchangeFailed", "Discord authentication failed", HttpStatusCode.Unauthorized,
            "Could not exchange the Discord authorization code.");
}

public static class AccountError
{
    public static OpenShockProblem NotLinked =>
        new("Account.NotLinked", "Account not linked", HttpStatusCode.BadRequest, "Link your account first.");

    public static OpenShockProblem InvalidApiServer =>
        new("Account.InvalidApiServer", "Invalid API server", HttpStatusCode.BadRequest, "Invalid API Server URL");

    public static OpenShockProblem MissingApiToken =>
        new("Account.MissingApiToken", "Missing API token", HttpStatusCode.BadRequest, "Missing API token");

    public static OpenShockProblem OpenShockAuthFailed(string detail) =>
        new("Account.OpenShockAuthFailed", "OpenShock authentication failed", HttpStatusCode.Unauthorized, detail);

    public static OpenShockProblem OpenShockUnreachable =>
        new("Account.OpenShockUnreachable", "OpenShock server unreachable", HttpStatusCode.BadGateway,
            "Error while contacting the OpenShock server.");
}

public static class ControlError
{
    public static OpenShockProblem InvalidIntensity =>
        new("Control.InvalidIntensity", "Invalid intensity", HttpStatusCode.BadRequest,
            "Intensity must be between 1 and 100.");

    public static OpenShockProblem InvalidDuration =>
        new("Control.InvalidDuration", "Invalid duration", HttpStatusCode.BadRequest,
            "Duration must be between 0.3 and 30 seconds.");

    public static OpenShockProblem TargetNotLinked =>
        new("Control.TargetNotLinked", "Target not linked", HttpStatusCode.BadRequest,
            "Target has not linked their account.");

    public static OpenShockProblem TargetNoShockers =>
        new("Control.TargetNoShockers", "Target has no shockers", HttpStatusCode.BadRequest,
            "Target has no shockers configured.");

    public static OpenShockProblem NotAllowed =>
        new("Control.NotAllowed", "Not allowed", HttpStatusCode.Forbidden,
            "You are not allowed to shock this user.");

    public static OpenShockProblem ShockerNotFound =>
        new("Control.ShockerNotFound", "Shocker not found", HttpStatusCode.NotFound,
            "The target's shocker was not found. They should re-run shocker setup.");

    public static OpenShockProblem ShockerPaused =>
        new("Control.ShockerPaused", "Shocker paused", HttpStatusCode.Conflict,
            "The target's shocker is paused.");

    public static OpenShockProblem ShockerNoPermission =>
        new("Control.ShockerNoPermission", "No shocker permission", HttpStatusCode.Forbidden,
            "The OpenShock server reported no permission for the shocker.");

    public static OpenShockProblem TargetUnauthenticated =>
        new("Control.TargetUnauthenticated", "Target unauthenticated", HttpStatusCode.Unauthorized,
            "The target's OpenShock connection is not authenticated.");
}

public static class WhitelistError
{
    public static OpenShockProblem CannotWhitelistSelf =>
        new("Whitelist.CannotWhitelistSelf", "Cannot whitelist yourself", HttpStatusCode.BadRequest,
            "You cannot whitelist yourself.");
}
