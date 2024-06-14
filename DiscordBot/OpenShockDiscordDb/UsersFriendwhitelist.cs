using System;
using System.Collections.Generic;

namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class UsersFriendwhitelist
{
    public decimal User { get; set; }

    public decimal WhitelistedFriend { get; set; }

    public DateTime CreatedOn { get; set; }
}
