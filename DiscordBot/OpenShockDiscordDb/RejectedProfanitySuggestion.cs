namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class RejectedProfanitySuggestion
{
    public long Id { get; set; }

    public string Trigger { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public ulong SuggestedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
