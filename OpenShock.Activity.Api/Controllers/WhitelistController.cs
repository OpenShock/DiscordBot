using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Problems;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.Activity.Api.Controllers;

public sealed class WhitelistController : ActivityControllerBase
{
    private readonly OpenShockDiscordContext _db;

    public WhitelistController(OpenShockDiscordContext db) => _db = db;

    /// <summary>The friends the caller allows to shock them.</summary>
    [HttpGet("whitelist")]
    [ProducesResponseType<WhitelistEntryDto[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var friends = await _db.UsersFriendwhitelists
            .Where(x => x.User == DiscordId)
            .Select(x => new WhitelistEntryDto(x.WhitelistedFriend.ToString()))
            .ToListAsync(ct);
        return Ok(friends);
    }

    [HttpPost("whitelist/{friendId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Add(ulong friendId, CancellationToken ct)
    {
        if (friendId == DiscordId) return Problem(WhitelistError.CannotWhitelistSelf);

        var exists = await _db.UsersFriendwhitelists
            .AnyAsync(x => x.User == DiscordId && x.WhitelistedFriend == friendId, ct);
        if (!exists)
        {
            _db.UsersFriendwhitelists.Add(new UsersFriendwhitelist { User = DiscordId, WhitelistedFriend = friendId });
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    [HttpDelete("whitelist/{friendId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(ulong friendId, CancellationToken ct)
    {
        await _db.UsersFriendwhitelists
            .Where(x => x.User == DiscordId && x.WhitelistedFriend == friendId)
            .ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
