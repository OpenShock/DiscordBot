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
    private static readonly HashSet<string> _containedProfanitiesSet = new(_containedProfanities.Keys);

    private static readonly Dictionary<string, float> _standaloneProfanities = new() {
        {"ass", 1f },
        {"cock", 1f },
        {"damn", 1f },
        {"crap", 1f },
        {"piss", 1f },
        {"anal", 1f },
        { "cum", 1f },
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
