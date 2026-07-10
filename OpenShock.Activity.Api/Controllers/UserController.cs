using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Problems;
using OpenShock.Activity.Api.Realtime;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Models;
using DbUser = OpenShock.DiscordBot.OpenShockDiscordDb.User;

namespace OpenShock.Activity.Api.Controllers;

public sealed class UserController : ActivityControllerBase
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<UserController> _logger;

    public UserController(OpenShockDiscordContext db, ILogger<UserController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await GetSelfAsync(ct));

    // Link (or re-link) an OpenShock account — mirrors the bot's /setup connection flow.
    [HttpPost("link")]
    [ProducesResponseType<LinkResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status502BadGateway, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Link(LinkRequest req, CancellationToken ct)
    {
        if (!Uri.TryCreate(req.ApiServer, UriKind.Absolute, out var serverUri))
            return Problem(AccountError.InvalidApiServer);
        if (string.IsNullOrWhiteSpace(req.ApiToken))
            return Problem(AccountError.MissingApiToken);

        var client = new OpenShockApiClient(new ApiClientOptions { Server = serverUri, Token = req.ApiToken });

        SelfResponse self;
        try
        {
            var res = await client.GetSelf(ct);
            if (res.IsT1) return Problem(AccountError.OpenShockAuthFailed("Authentication failed. Check your API token."));
            self = res.AsT0.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while validating OpenShock connection for {Server}", serverUri);
            return Problem(AccountError.OpenShockUnreachable);
        }

        var existing = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == DiscordId, ct);
        if (existing is null)
        {
            _db.Users.Add(new DbUser
            {
                DiscordId = DiscordId,
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

        await _db.SaveChangesAsync(ct);
        return Ok(new LinkResponse(self.Name));
    }

    [HttpDelete("link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlink(CancellationToken ct)
    {
        await _db.UsersShockers.Where(x => x.User == DiscordId).ExecuteDeleteAsync(ct);
        await _db.UsersFriendwhitelists.Where(x => x.User == DiscordId).ExecuteDeleteAsync(ct);
        await _db.Users.Where(x => x.DiscordId == DiscordId).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpGet("consent")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetConsent(CancellationToken ct)
        => Ok(await GetSelfAsync(ct));

    [HttpPut("consent")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> SetConsent(ConsentRequest req,
        [FromServices] IHubContext<RoomHub, IRoomClient> hub, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == DiscordId, ct);
        if (u is null) return Problem(AccountError.NotLinked);

        u.AllowRoomShocks = req.AllowRoomShocks;
        u.RoomMaxIntensity = Math.Clamp(req.RoomMaxIntensity, (byte)1, (byte)100);
        u.RoomMaxDurationMs = Math.Clamp(req.RoomMaxDurationMs, 300, 30000);
        await _db.SaveChangesAsync(ct);

        // Consent lives in the DB (control + roster read it there); just notify live clients.
        await hub.Clients.All.ConsentChanged(new ConsentChangedEvent(DiscordId, u.AllowRoomShocks));

        return Ok(new MeResponse(true, u.AllowRoomShocks, u.RoomMaxIntensity, u.RoomMaxDurationMs));
    }

    private async Task<MeResponse> GetSelfAsync(CancellationToken ct)
    {
        var u = await _db.Users.Where(x => x.DiscordId == DiscordId)
            .Select(x => new { x.AllowRoomShocks, x.RoomMaxIntensity, x.RoomMaxDurationMs })
            .FirstOrDefaultAsync(ct);

        return u is null
            ? new MeResponse(false, false, 30, 3000)
            : new MeResponse(true, u.AllowRoomShocks, u.RoomMaxIntensity, u.RoomMaxDurationMs);
    }
}
