using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Problems;

namespace OpenShock.Activity.Api.Controllers;

/// <summary>
/// Base for the Activity API's controllers. Requires an authenticated JWT session and exposes the
/// caller's Discord identity plus the <see cref="Problem(OpenShockProblem)"/> helper used to return the
/// RFC 7807 problem responses the frontend reads (mirroring the main OpenShock API).
/// </summary>
[ApiController]
[Authorize]
public abstract class ActivityControllerBase : ControllerBase
{
    /// <summary>Discord user id, from the JWT <c>sub</c> claim.</summary>
    protected ulong DiscordId => User.GetDiscordId();

    /// <summary>Discord display name, from the JWT <c>name</c> claim.</summary>
    protected string DisplayName => User.GetDisplayName();

    /// <summary>Returns a typed <see cref="OpenShockProblem"/> as an <c>application/problem+json</c> response.</summary>
    [NonAction]
    protected ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);
}
