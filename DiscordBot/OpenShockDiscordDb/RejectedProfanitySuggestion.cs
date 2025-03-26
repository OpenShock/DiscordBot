namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class RejectedProfanitySuggestion
{
    public ulong Id { get; set; }

    public string Trigger { get; set; }

    public string Reason { get; set; }

    public string LanguageCode { get; set; }

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
