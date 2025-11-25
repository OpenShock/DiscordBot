namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class User
{
    public ulong DiscordId { get; set; }

    public Guid OpenshockId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string ApiServer { get; set; } = null!;

    public string ApiKey { get; set; } = null!;
    
    public bool ProfanityShocking { get; set; }

    public virtual ICollection<UsersFriendwhitelist> UsersFriendwhitelists { get; set; } = new List<UsersFriendwhitelist>();

    public virtual ICollection<UsersShocker> UsersShockers { get; set; } = new List<UsersShocker>();
}
