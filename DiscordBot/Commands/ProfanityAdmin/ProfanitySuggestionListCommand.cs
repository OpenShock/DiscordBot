using System.Globalization;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

public sealed partial class ProfanityAdminGroup
{
    public sealed partial class SuggestionGroup
    {
        [SlashCommand("list", "List submitted profanity suggestions (admin only).")]
        public async Task ProfanitySuggestionListCommand()
        {
            await DeferAsync(ephemeral: true);

            if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
            {
                await FollowupAsync("You are not an administrator.", ephemeral: true);
                return;
            }

            var suggestions = await _db.ProfanitySuggestions
                .OrderByDescending(s => s.SuggestedAt)
                .Take(10)
                .ToListAsync();

            if (suggestions.Count == 0)
            {
                await FollowupAsync("📭 No pending suggestions found.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("📝 Pending Profanity Suggestions")
                .WithColor(Color.Orange)
                .WithFooter($"Showing {suggestions.Count} unreviewed suggestion(s)")
                .WithCurrentTimestamp();

            foreach (var s in suggestions)
            {
                embed.AddField(
                    $"ID {s.Id} • `{s.Trigger}`",
                    $"🧾 {s.Comment}\n🌐 `{CultureInfo.GetCultureInfo(s.LanguageCode).EnglishName}` • 👤 <@{s.SuggestedByUserId}> • ⏱ <t:{s.SuggestedAt.ToUnixTimeSeconds()}:R>",
                    inline: false
                );
            }

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}