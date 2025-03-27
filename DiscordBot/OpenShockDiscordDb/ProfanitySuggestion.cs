namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class ProfanitySuggestion
{
    public long Id { get; set; }

    public required string Trigger { get; set; }

    public required string Comment { get; set; }

    public required string LanguageCode { get; set; }

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset SuggestedAt { get; set; }
}
