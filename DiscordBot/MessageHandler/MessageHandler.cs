using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private static readonly Dictionary<string, float> _containedProfanities = new() {
        { "arduino",    0.2f },
        { "bastard",    0.4f },
        { "bitch",      0.5f },
        { "cocksucker", 0.9f },
        { "cunt",       0.5f },
        { "damnit",     0.3f },
        { "dick",       0.6f },
        { "penis",      0.5f },
        { "pishock",    0.2f },
        { "pussy",      0.5f },
        { "retard",     1.0f },
        { "shit",       0.3f },
        { "slut",       0.6f },
        { "fuck",       0.3f },
    };
    private static readonly HashSet<string> _containedProfanitiesSet = new(_containedProfanities.Keys);

    private static readonly Dictionary<string, float> _standaloneProfanities = new() {
        { "anal",        0.6f },
        { "cock",        0.5f },
        { "crap",        0.3f },
        { "cum",         0.4f },
        { "damn",        0.3f },
        { "micropython", 0.5f },
        { "piss",        0.3f },
        { "ass",         0.2f },
    };
    private static readonly HashSet<string> _standaloneProfanitiesSet = new(_standaloneProfanities.Keys);

    private static bool TryGetProfanityWeight(string str, out float weight)
    {
        weight = 0;

        if (string.IsNullOrEmpty(str)) return false;

        // Whole string contains word check
        foreach (var profanity in _containedProfanities)
        {
            if (str.AsSpan().Contains(profanity.Key, StringComparison.OrdinalIgnoreCase))
            {
                weight = profanity.Value;
                return true;
            }
        }

        str = str.ToLower();

        // Words of string matches words check
        var words = str.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (_standaloneProfanities.TryGetValue(word, out weight))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // If the channel name does not contain "bot", ignore the message
        if (!message.Channel.Name.Contains("bot", StringComparison.OrdinalIgnoreCase)) return;

        // Check if the message contains a swear word
        if (TryGetProfanityWeight(message.Content, out float weight))
        {
            // Respond to the message
            await message.Channel.SendMessageAsync($"Profanity detected! Weight: {weight}");
        }
    }
}
