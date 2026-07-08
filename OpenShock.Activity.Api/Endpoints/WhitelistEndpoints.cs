using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.Activity.Api.Endpoints;

public static class WhitelistEndpoints
{
    public static void MapWhitelistEndpoints(this IEndpointRouteBuilder app)
    {
        // The friends the caller allows to shock them (target whitelists caller semantics).
        app.MapGet("/whitelist", async (ClaimsPrincipal principal, OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var friends = await db.UsersFriendwhitelists
                .Where(x => x.User == id)
                .Select(x => new WhitelistEntryDto(x.WhitelistedFriend.ToString()))
                .ToListAsync(ct);
            return Results.Ok(friends);
        });

        app.MapPost("/whitelist/{friendId}", async (ulong friendId, ClaimsPrincipal principal,
            OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            if (friendId == id) return Results.BadRequest(new { error = "You cannot whitelist yourself." });

            var exists = await db.UsersFriendwhitelists
                .AnyAsync(x => x.User == id && x.WhitelistedFriend == friendId, ct);
            if (!exists)
            {
                db.UsersFriendwhitelists.Add(new UsersFriendwhitelist { User = id, WhitelistedFriend = friendId });
                await db.SaveChangesAsync(ct);
            }

            return Results.NoContent();
        });

        app.MapDelete("/whitelist/{friendId}", async (ulong friendId, ClaimsPrincipal principal,
            OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            await db.UsersFriendwhitelists
                .Where(x => x.User == id && x.WhitelistedFriend == friendId)
                .ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });
    }
}
