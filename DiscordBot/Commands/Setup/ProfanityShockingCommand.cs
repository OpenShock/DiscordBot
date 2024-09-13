using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.DiscordBot.Commands.Setup;

public sealed partial class SetupCommands
{
    [SlashCommand("profanity", "Profanity filter shocking. Say a bad word or phrase and get shocked.")]
    public async Task ExecuteProfanityCommand(bool enabled)
    {
        await DeferAsync(ephemeral: Context.IsNotDm());
        var user = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id);

        // First time setup
        if (user == null)
        {
            await FollowupAsync("You need to link your account first. Run `/setup connection` to link your account.", ephemeral: Context.IsNotDm());
            return;
        }
        
        user.ProfanityShocking = enabled;
        await _db.SaveChangesAsync();
        
        await FollowupAsync("Profanity shocking is now " + (enabled ? "enabled" : "disabled"), ephemeral: Context.IsNotDm());
    }
}