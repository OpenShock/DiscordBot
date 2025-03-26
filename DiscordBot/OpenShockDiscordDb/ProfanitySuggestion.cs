namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class ProfanitySuggestion
{
    public long Id { get; set; }

    public string Trigger { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset SuggestedAt { get; set; }
}
