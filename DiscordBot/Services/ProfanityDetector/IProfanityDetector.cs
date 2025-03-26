namespace OpenShock.DiscordBot.Services.ProfanityDetector;

public interface IProfanityDetector
{
    Task LoadProfanityRulesAsync();
    bool TryGetProfanityWeight(string input, out int matchCount, out float totalSeverity);
}