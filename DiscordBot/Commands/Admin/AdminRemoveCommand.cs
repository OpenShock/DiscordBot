using Discord.Interactions;
using Discord.WebSocket;

namespace OpenShock.DiscordBot.Commands.Admin;

public sealed partial class AdminGroup
{
    [SlashCommand("remove", "Remove an administrator from the bot.")]
    public async Task AdminRemoveCommand(SocketUser user)
    {
        await DeferAsync(ephemeral: true);

        if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

        var admin = _db.Administrators.FirstOrDefault(a => a.DiscordId == user.Id);

        if (admin == null)
        {
            await FollowupAsync("That user is not an administrator.", ephemeral: true);
            return;
        }

        if (!admin.IsRemovable)
        {
            await FollowupAsync("This administrator cannot be removed.", ephemeral: true);
            return;
        }

        _db.Administrators.Remove(admin);
        await _db.SaveChangesAsync();

        await FollowupAsync($"Removed {user.Mention} from administrators.", ephemeral: true);
    }
}
