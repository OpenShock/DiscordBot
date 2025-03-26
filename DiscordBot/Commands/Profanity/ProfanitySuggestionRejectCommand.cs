using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    public sealed partial class SuggestionGroup
    {
        [SlashCommand("reject", "Reject a profanity suggestion (admin only).")]
        public async Task ProfanitySuggestionRejectCommand(ulong id, string reason, bool? matchwholeword, string? validationRegex, string? category, string? comment)
        {
            if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
            {
                await FollowupAsync("You are not an administrator.", ephemeral: true);
                return;
            }

            var suggestion = await _db.ProfanitySuggestions.FirstOrDefaultAsync(s => s.Id == id);
            if (suggestion == null)
            {
                await RespondAsync("❌ Suggestion not found or already reviewed.", ephemeral: true);
                return;
            }

            var rejection = new RejectedProfanitySuggestion
            {
                Trigger = suggestion.Trigger,
                Reason = reason,
                LanguageCode = suggestion.LanguageCode,
                SuggestedByUserId = suggestion.SuggestedByUserId,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _db.RejectedProfanitySuggestions.Add(rejection);
            _db.Remove(suggestion);

            await _db.SaveChangesAsync();

            await RespondAsync($"🚫 Suggestion for `{suggestion.Trigger}` rejected.", ephemeral: true);
        }
    }
}