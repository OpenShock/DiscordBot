using Discord;
using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private static ProfanityFilter.ProfanityFilter ProfanityFilter { get; } = new();
    public static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        var lcWordsSet = message.Content.ToLower().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        // Check if the message contains a swear word
        if (ProfanityFilter.ContainsProfanity(message.Content))
        {
            await message.AddReactionAsync(new Emoji("😠"));
        }
    }
}
