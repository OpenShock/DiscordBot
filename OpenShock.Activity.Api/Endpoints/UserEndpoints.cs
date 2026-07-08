using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.Activity.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (ClaimsPrincipal principal, OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var u = await db.Users.Where(x => x.DiscordId == id)
                .Select(x => new { x.AllowRoomShocks, x.RoomMaxIntensity, x.RoomMaxDurationMs })
                .FirstOrDefaultAsync(ct);

            return Results.Ok(u is null
                ? new MeResponse(false, false, 30, 3000)
                : new MeResponse(true, u.AllowRoomShocks, u.RoomMaxIntensity, u.RoomMaxDurationMs));
        });

        // Link (or re-link) an OpenShock account — mirrors the bot's /setup connection flow.
        app.MapPost("/link", async (LinkRequest req, ClaimsPrincipal principal, OpenShockDiscordContext db,
            ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("Link");

            if (!Uri.TryCreate(req.ApiServer, UriKind.Absolute, out var serverUri))
                return Results.BadRequest(new { error = "Invalid API Server URL" });
            if (string.IsNullOrWhiteSpace(req.ApiToken))
                return Results.BadRequest(new { error = "Missing API token" });

            var client = new OpenShockApiClient(new ApiClientOptions { Server = serverUri, Token = req.ApiToken });

            SelfResponse self;
            try
            {
                var res = await client.GetSelf();
                if (res.IsT1)
                    return Results.Json(new { error = "Authentication failed. Check your API token." }, statusCode: 401);
                self = res.AsT0.Value;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while validating OpenShock connection for {Server}", serverUri);
                return Results.Json(new { error = "Error while contacting the OpenShock server." }, statusCode: 502);
            }

            var id = principal.GetDiscordId();
            var existing = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == id, ct);
            if (existing is null)
            {
                db.Users.Add(new User
                {
                    DiscordId = id,
                    OpenshockId = self.Id,
                    ApiKey = req.ApiToken,
                    ApiServer = serverUri.ToString()
                });
            }
            else
            {
                existing.OpenshockId = self.Id;
                existing.ApiKey = req.ApiToken;
                existing.ApiServer = serverUri.ToString();
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new LinkResponse(self.Name));
        });

        app.MapDelete("/link", async (ClaimsPrincipal principal, OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            await db.UsersShockers.Where(x => x.User == id).ExecuteDeleteAsync(ct);
            await db.UsersFriendwhitelists.Where(x => x.User == id).ExecuteDeleteAsync(ct);
            await db.Users.Where(x => x.DiscordId == id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        app.MapGet("/consent", async (ClaimsPrincipal principal, OpenShockDiscordContext db, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var u = await db.Users.Where(x => x.DiscordId == id)
                .Select(x => new { x.AllowRoomShocks, x.RoomMaxIntensity, x.RoomMaxDurationMs })
                .FirstOrDefaultAsync(ct);
            return Results.Ok(u is null
                ? new MeResponse(false, false, 30, 3000)
                : new MeResponse(true, u.AllowRoomShocks, u.RoomMaxIntensity, u.RoomMaxDurationMs));
        });

        app.MapPut("/consent", async (ConsentRequest req, ClaimsPrincipal principal, OpenShockDiscordContext db,
            RoomRegistry registry, IHubContext<RoomHub> hub, CancellationToken ct) =>
        {
            var id = principal.GetDiscordId();
            var u = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == id, ct);
            if (u is null) return Results.BadRequest(new { error = "Link your account first." });

            u.AllowRoomShocks = req.AllowRoomShocks;
            u.RoomMaxIntensity = Math.Clamp(req.RoomMaxIntensity, (byte)1, (byte)100);
            u.RoomMaxDurationMs = Math.Clamp(req.RoomMaxDurationMs, 300, 30000);
            await db.SaveChangesAsync(ct);

            registry.UpdateConsent(id, u.AllowRoomShocks);
            await hub.Clients.All.SendAsync(RoomEvents.ConsentChanged, new ConsentChangedEvent(id, u.AllowRoomShocks), ct);

            return Results.Ok(new MeResponse(true, u.AllowRoomShocks, u.RoomMaxIntensity, u.RoomMaxDurationMs));
        });
    }
}
