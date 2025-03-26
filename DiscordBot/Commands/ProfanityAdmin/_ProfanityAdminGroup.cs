using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.Commands.Admin;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services.ProfanityDetector;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

[CommandContextType(InteractionContextType.Guild)]
[IntegrationType(ApplicationIntegrationType.GuildInstall)]
[Group("profanityadmin", "Manage bot administrators")]
public sealed partial class ProfanityAdminGroup : InteractionModuleBase<SocketInteractionContext>
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<AdminGroup> _logger;
    private readonly IProfanityDetector _profanityDetector;

    /// <summary>
    /// Default constructor for the ProfanityGroup
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    /// <param name="profanityDetector"></param>
    public ProfanityAdminGroup(OpenShockDiscordContext db, ILogger<AdminGroup> logger, IProfanityDetector profanityDetector)
    {
        _db = db;
        _logger = logger;
        _profanityDetector = profanityDetector;
    }
}