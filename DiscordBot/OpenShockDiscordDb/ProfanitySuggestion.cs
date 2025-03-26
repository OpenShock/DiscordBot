namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class ProfanitySuggestion
{
    public ulong Id { get; set; }

    public string Trigger { get; set; }

    public string Comment { get; set; }

    public string LanguageCode { get; set; }

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset SuggestedAt { get; set; }
}
