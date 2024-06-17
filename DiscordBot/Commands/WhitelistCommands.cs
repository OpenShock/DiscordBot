using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
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
        var alreadyWhitelisted = await _db.UsersFriendwhitelists.FirstOrDefaultAsync(x =>
            x.User == Context.User.Id && x.WhitelistedFriend == friend.Id);

        if (alreadyWhitelisted != null)
        {
            _db.UsersFriendwhitelists.Add(new UsersFriendwhitelist()
            {
                User = Context.User.Id,
                WhitelistedFriend = friend.Id
            });
        }

        await _db.SaveChangesAsync();
    }
}