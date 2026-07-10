using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.Activity.Api.Endpoints;

public static class ShockerEndpoints
{
    public static void MapShockerEndpoints(this IEndpointRouteBuilder app)
    {
        // List the caller's OpenShock shockers with which ones are enabled for the bot/activity.
        app.MapGet("/shockers", async (ClaimsPrincipal principal, OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == id, ct);
            if (user is null) return Results.BadRequest(new { error = "Link your account first." });

            var res = await user.GetApiClient().GetOwnShockers(ct);
            if (res.IsT1) return Results.Json(new { error = "OpenShock authentication failed." }, statusCode: 401);

            var enabled = (await db.UsersShockers.Where(x => x.User == id).Select(x => x.ShockerId).ToListAsync(ct))
                .ToHashSet();

            var dtos = res.AsT0.Value
                .SelectMany(hub => hub.Shockers.Select(s => new ShockerDto(s.Id, s.Name, hub.Name, enabled.Contains(s.Id))))
                .ToArray();

            return Results.Ok(dtos);
        });

        // Reconcile which shockers are enabled. Only ids the user actually owns are accepted.
        app.MapPut("/shockers", async (SetShockersRequest req, ClaimsPrincipal principal, OpenShockDiscordContext db,
            CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == id, ct);
            if (user is null) return Results.BadRequest(new { error = "Link your account first." });

            var res = await user.GetApiClient().GetOwnShockers(ct);
            if (res.IsT1) return Results.Json(new { error = "OpenShock authentication failed." }, statusCode: 401);

            var owned = res.AsT0.Value.SelectMany(h => h.Shockers).Select(s => s.Id).ToHashSet();
            var desired = req.EnabledIds.Where(owned.Contains).ToHashSet();

            var current = await db.UsersShockers.Where(x => x.User == id).ToListAsync(ct);
            var currentIds = current.Select(x => x.ShockerId).ToHashSet();

            var toRemove = current.Where(x => !desired.Contains(x.ShockerId)).ToList();
            var toAdd = desired.Where(x => !currentIds.Contains(x))
                .Select(sid => new UsersShocker { User = id, ShockerId = sid });

            db.UsersShockers.RemoveRange(toRemove);
            db.UsersShockers.AddRange(toAdd);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}
