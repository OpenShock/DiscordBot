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

    private readonly record struct WordRange(int Start, int Length);

    private static float CalculateWeight(float accumulated, float weight) => accumulated == 0 ? weight : MathF.Max(accumulated, weight) + 0.1f;

    private static bool TryGetProfanityWeight(string str, out int count, out float weight)
    {
        count = 0;
        weight = 0;

        if (string.IsNullOrEmpty(str)) return false;

        ReadOnlySpan<char> strSpan = str.AsSpan();

        // Look trough string for all contained matches
        foreach (var item in _containedProfanities)
        {
            ReadOnlySpan<char> badWord = item.Key.AsSpan();
            float badWordWeight = item.Value;

            int start = 0;
            while (true) {
                int index = strSpan[start..].IndexOf(badWord, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;

                start = index + badWord.Length;

                count++;
                weight = CalculateWeight(weight, badWordWeight);
            }
        }

        // Collect all word ranges
        int rangeStart = 0;
        List<WordRange> wordRanges = [];
        for (int i = 0; i < strSpan.Length; i++)
        {
            char c = strSpan[i];

            if (c is not (' ' or '\t' or '\r' or '\n')) continue;

            if (rangeStart < i)
            {
                wordRanges.Add(new WordRange(rangeStart, i - rangeStart));
            }

            rangeStart = i + 1;
        }

        // Check if any of the words are standalone matches
        foreach (var item in _standaloneProfanities)
        {
            ReadOnlySpan<char> badWord = item.Key.AsSpan();
            float badWordWeight = item.Value;

            foreach (var wordRange in wordRanges)
            {
                if (strSpan.Slice(wordRange.Start, wordRange.Length).Equals(badWord, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    weight = CalculateWeight(weight, badWordWeight);
                }
            }
        }

        // Roof the weight to 1.0
        weight = MathF.Min(weight, 1.0f);

        return count > 0;
    }

    public static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // If the channel name does not contain "bot", ignore the message
        if (!message.Channel.Name.Contains("bot", StringComparison.OrdinalIgnoreCase)) return;

        // Check if the message contains a swear word
        if (TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            // Respond to the message
            await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")} with total weight: {weight}");
        }
    }
}
