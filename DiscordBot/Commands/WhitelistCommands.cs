using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
[Group("whitelist", "Whitelist commands for friends to use your shockers")]
public sealed class WhitelistCommands : InteractionModuleBase
{
    private readonly OpenShockDiscordContext _db;

    public WhitelistCommands(OpenShockDiscordContext db)
    {
        _db = db;
    }
    
    [SlashCommand("add", "Whitelist a friend to use your shockers")]
    public async Task AddWhitelist(SocketUser friend)
    {
        await DeferAsync(ephemeral: Context.IsNotDm());
        var alreadyWhitelisted = await _db.UsersFriendwhitelists.FirstOrDefaultAsync(x =>
            x.User == Context.User.Id && x.WhitelistedFriend == friend.Id);

        if (alreadyWhitelisted == null)
        {
            _db.UsersFriendwhitelists.Add(new UsersFriendwhitelist()
            {
                User = Context.User.Id,
                WhitelistedFriend = friend.Id
            });
            await _db.SaveChangesAsync();
            await FollowupAsync($"{friend.Mention} has been whitelisted.");
            return;
        }

        await FollowupAsync($"{friend.Mention} is already whitelisted.");
    }
    
    [SlashCommand("list", "List all friends that are whitelisted")]
    public async Task ListWhitelist()
    {
        await DeferAsync(ephemeral: Context.IsNotDm());
        var whitelistedFriends = await _db.UsersFriendwhitelists
            .Where(x => x.User == Context.User.Id)
            .Select(x => x.WhitelistedFriend)
            .ToListAsync();

        if (whitelistedFriends.Count < 1)
        {
            await FollowupAsync("You have no friends whitelisted.");
            return;
        }
        
        await FollowupAsync($"Whitelisted friends: {string.Join(", ", whitelistedFriends.Select(x => $"<@{x}>"))}");
    }
    
    [SlashCommand("remove", "Remove a friend from your whitelist")]
    public async Task RemoveWhitelist(SocketUser friend)
    {
        await DeferAsync(ephemeral: Context.IsNotDm());
        var alreadyWhitelisted = await _db.UsersFriendwhitelists.FirstOrDefaultAsync(x =>
            x.User == Context.User.Id && x.WhitelistedFriend == friend.Id);

        if (alreadyWhitelisted != null)
        {
            _db.UsersFriendwhitelists.Remove(alreadyWhitelisted);
            await _db.SaveChangesAsync();
            await FollowupAsync($"{friend.Mention} has been removed from the whitelist.");
            return;
        }

        await FollowupAsync($"{friend.Mention} is not whitelisted.");
    }
}