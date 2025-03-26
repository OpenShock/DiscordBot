using Discord.Interactions;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.Admin;

public sealed partial class AdminGroup
{
    [SlashCommand("claim", "Claim the bot as the first administrator.")]
    public async Task ClaimBot()
    {
        await DeferAsync(ephemeral: true);

        if (_db.Administrators.Any())
        {
            await FollowupAsync("An administrator already exists.", ephemeral: true);
            return;
        }

        var admin = new BotAdmin
        {
            DiscordId = Context.User.Id,
            IsRemovable = false,
            CreatedOn = DateTime.UtcNow
        };

        _db.Administrators.Add(admin);
        await _db.SaveChangesAsync();

        await FollowupAsync("You have successfully claimed the bot and are now an administrator.", ephemeral: true);
    }
}
