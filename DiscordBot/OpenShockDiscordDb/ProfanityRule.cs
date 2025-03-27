namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class ProfanityRule
{
    public long Id { get; set; }

    public required string Trigger { get; set; }

    public float SeverityScore { get; set; }

    public bool IsActive { get; set; }

    public bool MatchWholeWord { get; set; }

    public string? ValidationRegex { get; set; }

    public required string LanguageCode { get; set; }

    public string? Category { get; set; }

    public string? Comment { get; set; }

    public ulong? AddedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
