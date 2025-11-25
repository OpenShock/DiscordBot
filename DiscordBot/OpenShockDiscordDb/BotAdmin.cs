namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class BotAdmin
{
    public ulong DiscordId { get; set; }

    public bool IsRemovable { get; set; }

    public DateTime CreatedOn { get; set; }
}
