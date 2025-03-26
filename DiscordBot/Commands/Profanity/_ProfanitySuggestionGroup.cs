using Discord.Interactions;
using Discord;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.DiscordBot.Services.ProfanityDetector;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("suggestion", "Suggest new profanities")]
    public sealed partial class SuggestionGroup : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly OpenShockDiscordContext _db;
        private readonly IProfanityDetector _profanityDetector;
        private readonly ILogger<SuggestionGroup> _logger;

        /// <summary>
        /// Default constructor for the SuggestionGroup
        /// </summary>
        /// <param name="db"></param>
        /// <param name="profanityDetector"></param>
        /// <param name="logger"></param>
        public SuggestionGroup(OpenShockDiscordContext db, IProfanityDetector profanityDetector, ILogger<SuggestionGroup> logger)
        {
            _db = db;
            _profanityDetector = profanityDetector;
            _logger = logger;
        }
    }
}