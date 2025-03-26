using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [SlashCommand("remove", "Remove an existing profanity rule (admin only).")]
    public async Task ProfanityRemoveCommand(string trigger)
    {
        await DeferAsync(ephemeral: true);

        if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

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

        await _profanityDetector.LoadProfanityRulesAsync();
    }
}