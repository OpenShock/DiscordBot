using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private static readonly Dictionary<string, float> _containedProfanities = new() {
        { "arduino",  0.2f },
        { "bastard",  0.4f },
        { "bitch",    0.5f },
        { "cocksuck", 0.9f },
        { "cunt",     0.5f },
        { "damnit",   0.3f },
        { "dick",     0.6f },
        { "penis",    0.5f },
        { "pishock",  0.2f },
        { "pussy",    0.5f },
        { "retard",   1.0f },
        { "shit",     0.3f },
        { "slut",     0.6f },
        { "fuck",     0.3f },
        { "i use arch b", 0.9f }
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

    private static float CalculateWeight(float accumulated, float weight) => accumulated == 0 ? weight : MathF.Max(accumulated, weight) + 0.1f;

    private static void GetContainedWordsCountAndWeight(ReadOnlySpan<char> lowerCaseSpan, ref int count, ref float weight)
    {
        foreach (var item in _containedProfanities)
        {
            ReadOnlySpan<char> remaining = lowerCaseSpan;
            while (true)
            {
                int index = remaining.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;

                remaining = remaining[(index + item.Key.Length)..];

                count++;
                weight = CalculateWeight(weight, item.Value);
            }
        }
    }

    private readonly record struct WordRange(int Start, int End);
    private static List<WordRange> GetWordRanges(ReadOnlySpan<char> span)
    {
        List<WordRange> wordRanges = [];

        int rangeStart = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] is not (' ' or '\t' or '\r' or '\n')) continue;

            if (rangeStart < i)
            {
                wordRanges.Add(new WordRange(rangeStart, i));
            }

            rangeStart = i + 1;
        }

        if (rangeStart < span.Length)
        {
            wordRanges.Add(new WordRange(rangeStart, span.Length));
        }

        return wordRanges;
    }

    private static void GetStandaloneWordsCountAndWeight(ReadOnlySpan<char> lowerCaseSpan, ref int count, ref float weight)
    {
        List<WordRange> wordRanges = GetWordRanges(lowerCaseSpan);

        // Check if any of the words are standalone matches
        foreach (var item in _standaloneProfanities)
        {
            foreach (var wordRange in wordRanges)
            {
                if (lowerCaseSpan[wordRange.Start..wordRange.End].Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    weight = CalculateWeight(weight, item.Value);
                }
            }
        }
    }

    private static bool TryGetProfanityWeight(string str, out int count, out float weight)
    {
        count = 0;
        weight = 0;

        if (string.IsNullOrEmpty(str)) return false;

        str = str.ToLowerInvariant();
        ReadOnlySpan<char> strSpan = str.AsSpan();

        GetContainedWordsCountAndWeight(strSpan, ref count, ref weight);
        GetStandaloneWordsCountAndWeight(strSpan, ref count, ref weight);

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
            await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {weight * 100f}%");
        }
    }
}
