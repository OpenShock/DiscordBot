using Discord.Interactions;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    public sealed partial class SuggestionGroup
    {
        [SlashCommand("accept", "Accept a profanity suggestion (admin only).")]
        public async Task ProfanitySuggestionAcceptCommand(ulong id, float? severity, bool? matchwholeword, string? validationRegex, string? category, string? comment)
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
                await RespondAsync("❌ Suggestion not found or already reviewed.", ephemeral: true);
                return;
            }

            var rule = new ProfanityRule
            {
                Trigger = suggestion.Trigger,
                SeverityScore = severity ?? 0.5f,
                IsActive = true,
                MatchWholeWord = matchwholeword ?? true,
                ValidationRegex = validationRegex,
                LanguageCode = suggestion.LanguageCode,
                Category = category,
                Comment = comment,
                AddedByUserId = suggestion.SuggestedByUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _db.ProfanityRules.Add(rule);
            await _db.SaveChangesAsync();

            await RespondAsync($"✅ Suggestion accepted and rule created for `{rule.Trigger}`.", ephemeral: true);
        }
    }
}