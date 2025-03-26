using System.Text;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

public sealed partial class ProfanityAdminGroup
{
    [SlashCommand("remove", "Remove an existing profanity rule (admin only).")]
    public async Task ProfanityRemoveCommand(string trigger)
    {
        await DeferAsync(ephemeral: true);

        if (!Queryable.Any<BotAdmin>(_db.Administrators, a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

        trigger = trigger.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();

        var rule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<ProfanityRule>(_db.ProfanityRules, r => r.Trigger == trigger);
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