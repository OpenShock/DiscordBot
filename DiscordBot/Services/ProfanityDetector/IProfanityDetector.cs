namespace OpenShock.DiscordBot.Services;

public interface IProfanityDetector
{
    Task LoadProfanityRulesAsync();
    bool TryGetProfanityWeight(string input, out int matchCount, out float totalSeverity);
}