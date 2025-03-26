using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [SlashCommand("list", "List current profanity rules (admin only).")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ProfanityListCommand()
    {
        await DeferAsync(ephemeral: true);

        var rules = await _db.ProfanityRules
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        if (rules.Count == 0)
        {
            await FollowupAsync("📭 No active profanity rules found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("📚 Active Profanity Rules")
            .WithColor(Color.DarkRed)
            .WithFooter($"Showing latest {rules.Count} rule(s)")
            .WithCurrentTimestamp();

        foreach (var rule in rules)
        {
            embed.AddField(
                $"• `{rule.Trigger}` ({rule.LanguageCode})",
                $"Severity: {rule.SeverityScore}\nRegex: `{rule.ValidationRegex ?? "None"}`\nBy: <@{rule.AddedByUserId}>",
                inline: false
            );
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}