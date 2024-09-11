using Discord;
using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private static readonly Dictionary<string, float> _containedProfanities = new() {
        {"fuck", 1f },
        {"pishock", 1f },
        {"arduino", 1f },
        {"penis", 1f },
        {"pussy", 1f },
        {"damnit", 1f },
        {"cocksucker", 1f },
        {"retard", 1f },
        {"dick", 1f },
        {"bitch", 1f },
        {"bastard", 1f },
        {"cunt", 1f },
        {"slut", 1f },
        { "shit", 1f },
    };

    private static readonly Dictionary<string, float> _standaloneProfanities = new() {
        {"ass", 1f },
        {"cock", 1f },
        {"damn", 1f },
        {"crap", 1f },
        {"piss", 1f },
        {"anal", 1f },
        { "cum", 1f },
    };

    private static bool ContainsProfanities(string str)
    {
        if (_containedProfanities.Keys.Any(str.Contains))
        {
            return true;
        }

        var lcWordsSet = str.ToLower().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lcWordsSet.Any(_standaloneProfanities.ContainsKey))
        {
            return true;
        }

        return false;
    }

    public static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // Check if the message contains a swear word
        if (ContainsProfanities(message.Content))
        {
            await message.AddReactionAsync(new Emoji("😠"));
        }
    }
}
