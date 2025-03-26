using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [SlashCommand("remove", "Remove an existing profanity rule (admin only).")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ProfanityRemoveCommand(string trigger)
    {
        await DeferAsync(ephemeral: true);

        trigger = trigger.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();

        var rule = await _db.ProfanityRules.FirstOrDefaultAsync(r => r.Trigger == trigger);
        if (rule == null)
        {
            await FollowupAsync($"❌ No rule found for `{trigger}`.", ephemeral: true);
            return;
        }

        _db.ProfanityRules.Remove(rule);
        await _db.SaveChangesAsync();

        await FollowupAsync($"🗑 Rule for `{trigger}` removed.", ephemeral: true);
    }
}