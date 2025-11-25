namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class UsersShocker
{
    public ulong User { get; set; }

    public Guid ShockerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User UserNavigation { get; set; } = null!;
}
