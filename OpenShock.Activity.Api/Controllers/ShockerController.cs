using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Problems;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.Activity.Api.Controllers;

public sealed class ShockerController : ActivityControllerBase
{
    private readonly OpenShockDiscordContext _db;

    public ShockerController(OpenShockDiscordContext db) => _db = db;

    /// <summary>List the caller's OpenShock shockers with which ones are enabled for the activity.</summary>
    [HttpGet("shockers")]
    [ProducesResponseType<ShockerDto[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == DiscordId, ct);
        if (user is null) return Problem(AccountError.NotLinked);

        var res = await user.GetApiClient().GetOwnShockers(ct);
        if (res.IsT1) return Problem(AccountError.OpenShockAuthFailed("OpenShock authentication failed."));

        var enabled = (await _db.UsersShockers.Where(x => x.User == DiscordId).Select(x => x.ShockerId).ToListAsync(ct))
            .ToHashSet();

        var dtos = res.AsT0.Value
            .SelectMany(hub => hub.Shockers.Select(s => new ShockerDto(s.Id, s.Name, hub.Name, enabled.Contains(s.Id))))
            .ToArray();

        return Ok(dtos);
    }

    /// <summary>Reconcile which shockers are enabled. Only ids the user actually owns are accepted.</summary>
    [HttpPut("shockers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Set(SetShockersRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == DiscordId, ct);
        if (user is null) return Problem(AccountError.NotLinked);

        var res = await user.GetApiClient().GetOwnShockers(ct);
        if (res.IsT1) return Problem(AccountError.OpenShockAuthFailed("OpenShock authentication failed."));

        var owned = res.AsT0.Value.SelectMany(h => h.Shockers).Select(s => s.Id).ToHashSet();
        var desired = req.EnabledIds.Where(owned.Contains).ToHashSet();

        var current = await _db.UsersShockers.Where(x => x.User == DiscordId).ToListAsync(ct);
        var currentIds = current.Select(x => x.ShockerId).ToHashSet();

        var toRemove = current.Where(x => !desired.Contains(x.ShockerId)).ToList();
        var toAdd = desired.Where(x => !currentIds.Contains(x))
            .Select(sid => new UsersShocker { User = DiscordId, ShockerId = sid });

        _db.UsersShockers.RemoveRange(toRemove);
        _db.UsersShockers.AddRange(toAdd);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
