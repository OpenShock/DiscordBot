﻿using System;
using System.Collections.Generic;

namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class User
{
    public decimal DiscordId { get; set; }

    public Guid OpenshockId { get; set; }

    public DateTime CreatedOn { get; set; }

    public string ApiServer { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public virtual ICollection<GuildActiveShocker> GuildActiveShockers { get; set; } = new List<GuildActiveShocker>();
}
