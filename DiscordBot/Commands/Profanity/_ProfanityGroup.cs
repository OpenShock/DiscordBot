using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;

namespace OpenShock.DiscordBot.Commands.Profanity;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
[Group("profanity", "Suggest and manage profanity triggers")]
public sealed partial class ProfanityGroup : InteractionModuleBase<SocketInteractionContext>
{
    private readonly OpenShockDiscordContext _db;
    private readonly IProfanityDetector _profanityDetector;
    private readonly ILogger<ProfanityGroup> _logger;

    /// <summary>
    /// Default constructor for the ProfanityGroup
    /// </summary>
    /// <param name="db"></param>
    /// <param name="profanityDetector"></param>
    /// <param name="logger"></param>
    public ProfanityGroup(OpenShockDiscordContext db, IProfanityDetector profanityDetector, ILogger<ProfanityGroup> logger)
    {
        _db = db;
        _profanityDetector = profanityDetector;
        _logger = logger;
    }
}