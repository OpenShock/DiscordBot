using Discord;
using Discord.Interactions;

namespace OpenShock.DiscordBot.Utils;

public static class DiscordUtils
{
    public static bool IsNotDm(this IInteractionContext context)
    {
        return context.Interaction.ContextType != InteractionContextType.BotDm;
    }
}