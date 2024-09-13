using System.Buffers;

namespace OpenShock.DiscordBot.MessageHandler;

public static class ProfanityDetector
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
        { "damn",        0.15f },
        { "micropython", 0.5f },
        { "piss",        0.3f },
        { "ass",         0.2f },
        { "wtf",         0.15f },
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
    private static readonly SearchValues<char> _seperationValues = SearchValues.Create([' ', '\t', '\r', '\n', '?', '!', ',', '.']);
    private static List<WordRange> GetWordRanges(ReadOnlySpan<char> span)
    {
        List<WordRange> wordRanges = [];

        int spanIndex = 0;
        while (true)
        {
            // Find the next white space character
            int index = span.IndexOfAny(_seperationValues);
            if (index < 0)
            {
                // Add the remaining word range if the word is not empty
                if (span.Length > 0)
                {
                    wordRanges.Add(new WordRange(spanIndex, spanIndex + span.Length));
                }

                // Exit the loop, span is fully processed
                break;
            }

            // Skip the white space character if it is the first character in the span
            if (index == 0)
            {
                spanIndex++;
                span = span[1..];
                continue;
            }

            // Add the word range if the word is not empty
            wordRanges.Add(new WordRange(spanIndex, spanIndex + index));

            // Move the span index to after the white space character
            spanIndex += index + 1;
            span = span[(index + 1)..];
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

    public static bool TryGetProfanityWeight(string str, out int count, out float weight)
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
}
