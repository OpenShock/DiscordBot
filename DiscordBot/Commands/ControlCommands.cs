using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public sealed class ControlCommands : InteractionModuleBase
{
    [SlashCommand("shock", "Shock a friend that has whitelisted you before")]
    public async Task ShockCommand(SocketUser user)
    {
        await RespondAsync("Shocking " + user.Mention + "!");
    }
}