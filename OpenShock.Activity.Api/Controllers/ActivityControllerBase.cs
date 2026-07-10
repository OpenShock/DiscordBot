using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Activity.Api.Auth;
using OpenShock.Internal.Common;

namespace OpenShock.Activity.Api.Controllers;

/// <summary>
/// Base for the Activity API's controllers. Requires an authenticated JWT session and exposes the
/// caller's Discord identity. Inherits <see cref="OpenShockControllerBase"/> for the shared
/// <c>Problem(OpenShockProblem)</c> helper that returns the RFC 7807 problem responses the frontend
/// reads (mirroring the main OpenShock API).
/// </summary>
[ApiController]
[Authorize]
public abstract class ActivityControllerBase : OpenShockControllerBase
{
    /// <summary>Discord user id, from the JWT <c>sub</c> claim.</summary>
    protected ulong DiscordId => User.GetDiscordId();

    /// <summary>Discord display name, from the JWT <c>name</c> claim.</summary>
    protected string DisplayName => User.GetDisplayName();
}
