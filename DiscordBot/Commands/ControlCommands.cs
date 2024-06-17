using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public sealed class ControlCommands : InteractionModuleBase
{
    private readonly OpenShockDiscordContext _db;

    public ControlCommands(OpenShockDiscordContext db)
    {
        _db = db;
    }
    
    [SlashCommand("shock", "Shock a friend that has whitelisted you before")]
    public async Task ShockCommand(SocketUser user, [Range(1, 100)] byte intensity = 50, [Range(0.3, 30)] float duration = 5)
    {
        await RespondAsync("hiii lol");
        var friend = await _db.UsersFriendwhitelists.FirstOrDefaultAsync(x => x.WhitelistedFriend ==  Context.User.Id && x.User == user.Id);
        
        
    }
}