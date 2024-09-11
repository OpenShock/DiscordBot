using Discord;
using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private static readonly HashSet<string> _containedProfanities =
    [
        "fuck",
        "pishock",
        "arduino",
        "penis",
        "pussy",
        "cocksucker",
        "dick"
    ];

    private static readonly HashSet<string> _standaloneProfanities =
    [
        "shit",
        "fuck",
        "ass",
        "pishock",
        "arduino",
        "penis",
        "bitch",
        "bastard",
        "cock",
        "crap",
        "cunt",
        "damn",
        "dick",
        "pussy",
        "slut",
        "damn",
        "damnit",
        "cocksucker",
        "curwa",
        "dong",
        "jævla",
        "jävla",
        "kug",
        "kuk"
    ];

    private static bool ContainsProfanities(string str)
    {
        if (_containedProfanities.Any(str.Contains))
        {
            return true;
        }

        var lcWordsSet = str.ToLower().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (_standaloneProfanities.Overlaps(lcWordsSet))
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
