namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class User
{
    public ulong DiscordId { get; set; }

    public Guid OpenshockId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string ApiServer { get; set; } = null!;

    public string ApiKey { get; set; } = null!;
    
    public bool ProfanityShocking { get; set; }

    /// <summary>
    /// When true, any participant sharing the same Discord Activity room instance may shock this user
    /// without an explicit friend whitelist entry, subject to <see cref="RoomMaxIntensity"/> and
    /// <see cref="RoomMaxDurationMs"/>.
    /// </summary>
    public bool AllowRoomShocks { get; set; }

    /// <summary>Upper bound (1-100) applied to room-consent shocks.</summary>
    public byte RoomMaxIntensity { get; set; } = 30;

    /// <summary>Upper bound in milliseconds applied to room-consent shocks.</summary>
    public int RoomMaxDurationMs { get; set; } = 3000;

    /// <summary>
    /// True once the user granted the <c>application_identities.write</c> OAuth2 scope (via the Discord
    /// Activity), which is required before we may push stats to their Discord profile widget.
    /// </summary>
    public bool WidgetAuthorized { get; set; }

    public virtual ICollection<UsersFriendwhitelist> UsersFriendwhitelists { get; set; } = new List<UsersFriendwhitelist>();

    public virtual ICollection<UsersShocker> UsersShockers { get; set; } = new List<UsersShocker>();
}
