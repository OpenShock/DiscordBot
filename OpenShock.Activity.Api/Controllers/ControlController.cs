using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Problems;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;

namespace OpenShock.Activity.Api.Controllers;

public sealed class ControlController : ActivityControllerBase
{
    private readonly OpenShockDiscordContext _db;
    private readonly IOpenShockBackendService _control;
    private readonly IRoomRegistry _registry;
    private readonly IHubContext<RoomHub, IRoomClient> _hub;

    public ControlController(OpenShockDiscordContext db, IOpenShockBackendService control,
        IRoomRegistry registry, IHubContext<RoomHub, IRoomClient> hub)
    {
        _db = db;
        _control = control;
        _registry = registry;
        _hub = hub;
    }

    /// <summary>Shock/vibrate/sound a target — allowed for self, whitelisted friends, or (clamped) room consent.</summary>
    [HttpPost("control")]
    [ProducesResponseType<ControlResponseDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Control(ControlRequestDto req, CancellationToken ct)
    {
        var callerId = DiscordId;
        var callerName = DisplayName;

        if (req.Intensity is < 1 or > 100)
            return Problem(ControlError.InvalidIntensity);
        if (req.Duration is < 0.3f or > 30f)
            return Problem(ControlError.InvalidDuration);

        var target = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == req.TargetDiscordId, ct);
        if (target is null) return Problem(ControlError.TargetNotLinked);

        var shockers = await _db.UsersShockers.Where(x => x.User == req.TargetDiscordId).ToListAsync(ct);
        if (shockers.Count == 0) return Problem(ControlError.TargetNoShockers);

        byte intensity = req.Intensity;
        var durationMs = (ushort)(req.Duration * 1000);

        // Permission: self, explicit whitelist, or room-consent (clamped to the target's caps).
        bool allowed;
        if (callerId == req.TargetDiscordId)
        {
            allowed = true;
        }
        else
        {
            var whitelisted = await _db.UsersFriendwhitelists
                .AnyAsync(x => x.User == req.TargetDiscordId && x.WhitelistedFriend == callerId, ct);
            if (whitelisted)
            {
                allowed = true;
            }
            else if (target.AllowRoomShocks &&
                     await _registry.AreInSameRoomAsync(req.InstanceId, callerId, req.TargetDiscordId, ct))
            {
                allowed = true;
                intensity = Math.Min(intensity, target.RoomMaxIntensity);
                durationMs = Math.Min(durationMs, (ushort)target.RoomMaxDurationMs);
            }
            else
            {
                allowed = false;
            }
        }

        if (!allowed)
            return Problem(ControlError.NotAllowed);

        var result = await _control.ControlShockers(target, shockers, req.Type, intensity, durationMs, req.Mode,
            $"{callerName} [Discord Activity]");

        return await result.Match<Task<IActionResult>>(
            async _ =>
            {
                await _hub.Clients.Group(req.InstanceId).ShockDelivered(
                    new ShockDeliveredEvent(callerId, callerName, req.TargetDiscordId, intensity,
                        durationMs / 1000f, req.Type.ToString()));
                return Ok(new ControlResponseDto(intensity, durationMs / 1000f));
            },
            notFound => Task.FromResult<IActionResult>(Problem(ControlError.ShockerNotFound)),
            paused => Task.FromResult<IActionResult>(Problem(ControlError.ShockerPaused)),
            noPermission => Task.FromResult<IActionResult>(Problem(ControlError.ShockerNoPermission)),
            unauthenticated => Task.FromResult<IActionResult>(Problem(ControlError.TargetUnauthenticated)));
    }
}
