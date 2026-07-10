namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class UsersFriendwhitelist
{
    public ulong User { get; set; }

    public ulong WhitelistedFriend { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual User UserNavigation { get; set; } = null!;
}
