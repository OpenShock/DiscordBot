using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

public sealed partial class ProfanityAdminGroup
{
    [SlashCommand("list", "List current profanity rules (admin only).")]
    public async Task ProfanityListCommand()
    {
        await DeferAsync(ephemeral: true);

        if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

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