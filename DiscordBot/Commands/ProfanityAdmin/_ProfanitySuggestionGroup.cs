using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services.ProfanityDetector;

namespace OpenShock.DiscordBot.Commands.ProfanityAdmin;

public sealed partial class ProfanityAdminGroup
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("suggestion", "Suggestions")]
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