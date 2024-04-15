using System;
using System.Collections.Generic;

namespace OpenShock.DiscordBot.OpenShockDiscordDb;

public partial class GuildActiveShocker
{
    public decimal GuildId { get; set; }

    public decimal DiscordId { get; set; }

    public Guid ShockerId { get; set; }

    public bool Paused { get; set; }

    public bool? PermSound { get; set; }

    public bool? PermVibrate { get; set; }

    public bool? PermShock { get; set; }

    public int? LimitDuration { get; set; }

    public short? LimitIntensity { get; set; }

    public virtual User Discord { get; set; } = null!;
}
