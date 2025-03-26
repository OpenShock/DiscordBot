using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.Admin;

[CommandContextType(InteractionContextType.Guild)]
[IntegrationType(ApplicationIntegrationType.GuildInstall)]
[Group("admin", "Manage bot administrators")]
public sealed partial class AdminGroup : InteractionModuleBase<SocketInteractionContext>
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<AdminGroup> _logger;

    /// <summary>
    /// Default constructor for the ProfanityGroup
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    public AdminGroup(OpenShockDiscordContext db, ILogger<AdminGroup> logger)
    {
        _db = db;
        _logger = logger;
    }
}