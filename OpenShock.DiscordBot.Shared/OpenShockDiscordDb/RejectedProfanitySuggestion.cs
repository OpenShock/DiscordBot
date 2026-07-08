namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class RejectedProfanitySuggestion
{
    public long Id { get; set; }

    public required string Trigger { get; set; }

    public required string Reason { get; set; }

    public required string LanguageCode { get; set; }

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
