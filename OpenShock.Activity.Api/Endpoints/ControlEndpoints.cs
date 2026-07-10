using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;

namespace OpenShock.Activity.Api.Endpoints;

public static class ControlEndpoints
{
    public static void MapControlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/control", async (ControlRequestDto req, ClaimsPrincipal principal, OpenShockDiscordContext db,
            IOpenShockBackendService control, IRoomRegistry registry, IHubContext<RoomHub, IRoomClient> hub, CancellationToken ct) =>
        {
            var callerId = principal.GetDiscordId();
            var callerName = principal.GetDisplayName();

            if (req.Intensity is < 1 or > 100)
                return Results.BadRequest(new { error = "Intensity must be between 1 and 100." });
            if (req.Duration is < 0.3f or > 30f)
                return Results.BadRequest(new { error = "Duration must be between 0.3 and 30 seconds." });

            var target = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == req.TargetDiscordId, ct);
            if (target is null) return Results.BadRequest(new { error = "Target has not linked their account." });

            var shockers = await db.UsersShockers.Where(x => x.User == req.TargetDiscordId).ToListAsync(ct);
            if (shockers.Count == 0) return Results.BadRequest(new { error = "Target has no shockers configured." });

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
                var whitelisted = await db.UsersFriendwhitelists
                    .AnyAsync(x => x.User == req.TargetDiscordId && x.WhitelistedFriend == callerId, ct);
                if (whitelisted)
                {
                    allowed = true;
                }
                else if (target.AllowRoomShocks &&
                         await registry.AreInSameRoomAsync(req.InstanceId, callerId, req.TargetDiscordId, ct))
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
                return Results.Json(new { error = "You are not allowed to shock this user." }, statusCode: 403);

            var result = await control.ControlShockers(target, shockers, req.Type, intensity, durationMs, req.Mode,
                $"{callerName} [Discord Activity]");

            return await result.Match<Task<IResult>>(
                async _ =>
                {
                    await hub.Clients.Group(req.InstanceId).ShockDelivered(
                        new ShockDeliveredEvent(callerId, callerName, req.TargetDiscordId, intensity,
                            durationMs / 1000f, req.Type.ToString()));
                    return Results.Ok(new { intensity, durationSeconds = durationMs / 1000f });
                },
                notFound => Task.FromResult(Results.Json(
                    new { error = "The target's shocker was not found. They should re-run shocker setup." },
                    statusCode: 404)),
                paused => Task.FromResult(Results.Json(
                    new { error = "The target's shocker is paused." }, statusCode: 409)),
                noPermission => Task.FromResult(Results.Json(
                    new { error = "The OpenShock server reported no permission for the shocker." }, statusCode: 403)),
                unauthenticated => Task.FromResult(Results.Json(
                    new { error = "The target's OpenShock connection is not authenticated." }, statusCode: 401)));
        });
    }
}
