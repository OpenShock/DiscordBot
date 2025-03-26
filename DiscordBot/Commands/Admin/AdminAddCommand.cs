using Discord.Interactions;
using Discord.WebSocket;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.DiscordBot.Commands.Admin;

public sealed partial class AdminGroup
{
    [SlashCommand("add", "Add a new administrator to the bot.")]
    public async Task AdminAddCommand(SocketUser user)
    {
        await DeferAsync(ephemeral: Context.IsNotDm());

        if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

        if (_db.Administrators.Any(a => a.DiscordId == user.Id))
        {
            await FollowupAsync("That user is already an administrator.", ephemeral: true);
            return;
        }

        var admin = new BotAdmin
        {
            DiscordId = user.Id,
            IsRemovable = true,
            CreatedOn = DateTime.UtcNow
        };

        _db.Administrators.Add(admin);
        await _db.SaveChangesAsync();

        await FollowupAsync($"Added {user.Mention} as an administrator.", ephemeral: true);
    }
}
