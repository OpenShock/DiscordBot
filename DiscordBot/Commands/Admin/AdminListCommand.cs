using Discord.Interactions;

namespace OpenShock.DiscordBot.Commands.Admin;

public sealed partial class AdminGroup
{
    [SlashCommand("list", "List all administrators.")]
    public async Task AdminListCommand()
    {
        await DeferAsync(ephemeral: true);

        if (!_db.Administrators.Any(a => a.DiscordId == Context.User.Id))
        {
            await FollowupAsync("You are not an administrator.", ephemeral: true);
            return;
        }

        var admins = _db.Administrators.ToList();

        if (admins.Count == 0)
        {
            await FollowupAsync("There are no administrators yet.", ephemeral: true);
            return;
        }

        var adminMentions = admins
            .Select(a => $"<@{a.DiscordId}> {(a.IsRemovable ? "" : "(Owner)")}")
            .ToList();

        await FollowupAsync($"**Administrators:**\n{string.Join("\n", adminMentions)}", ephemeral: true);
    }
}
