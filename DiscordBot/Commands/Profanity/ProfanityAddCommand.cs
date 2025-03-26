using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using System.Text;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [SlashCommand("add", "Add a profanity rule directly (admin only).")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ProfanityAddCommand(
    string trigger,
    string language,
    float? severity = 0.5f,
    bool? matchwholeword = true,
    string? validationRegex = null,
    string? category = null,
    string? comment = null)
    {
        await DeferAsync(ephemeral: true);

        trigger = trigger.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();

        if (!RelevantCultures.TryGetValue(language.Trim().ToLower(), out var languageCode))
        {
            await FollowupAsync($"❌ `{language}` is not a valid language.");
            return;
        }

        if (await _db.ProfanityRules.AnyAsync(r => r.Trigger == trigger))
        {
            await FollowupAsync($"⚠️ `{trigger}` already exists as a rule.");
            return;
        }

        var rule = new ProfanityRule
        {
            Trigger = trigger,
            LanguageCode = languageCode,
            SeverityScore = severity ?? 0.5f,
            MatchWholeWord = matchwholeword ?? true,
            ValidationRegex = validationRegex,
            Category = category,
            Comment = comment,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            AddedByUserId = Context.User.Id
        };

        _db.ProfanityRules.Add(rule);
        await _db.SaveChangesAsync();

        await FollowupAsync($"✅ Rule for `{trigger}` added.");
    }
}