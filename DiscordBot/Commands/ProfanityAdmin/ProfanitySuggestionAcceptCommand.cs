using Discord.Interactions;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

public sealed partial class ProfanityAdminGroup
{
    public sealed partial class SuggestionGroup
    {
        [SlashCommand("accept", "Accept a profanity suggestion (admin only).")]
        public async Task ProfanitySuggestionAcceptCommand(long id, float severity = 0.5f, bool matchwholeword = true, string? validationRegex = null, string? category = null, string? comment = null)
        {
            await DeferAsync(ephemeral: true);

            if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
            {
                await FollowupAsync("You are not an administrator.", ephemeral: true);
                return;
            }

            var suggestion = await _db.ProfanitySuggestions.FindAsync(id);
            if (suggestion == null)
            {
                await FollowupAsync("❌ Suggestion not found or already reviewed.", ephemeral: true);
                return;
            }

            var rule = new ProfanityRule
            {
                Trigger = suggestion.Trigger,
                SeverityScore = severity,
                IsActive = true,
                MatchWholeWord = matchwholeword,
                ValidationRegex = validationRegex,
                LanguageCode = suggestion.LanguageCode,
                Category = category,
                Comment = comment,
                AddedByUserId = suggestion.SuggestedByUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _db.ProfanityRules.Add(rule);
            await _db.SaveChangesAsync();

            await FollowupAsync($"✅ Suggestion accepted and rule created for `{rule.Trigger}`.", ephemeral: true);

            await _profanityDetector.LoadProfanityRulesAsync();
        }
    }
}