using Discord.Interactions;
using Discord;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Commands.Profanity;

public sealed partial class ProfanityGroup
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("suggestion", "Suggest new profanities")]
    public sealed partial class SuggestionGroup : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly OpenShockDiscordContext _db;
        private readonly ILogger<SuggestionGroup> _logger;

        /// <summary>
        /// Default constructor for the SuggestionGroup
        /// </summary>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        public SuggestionGroup(OpenShockDiscordContext db, ILogger<SuggestionGroup> logger)
        {
            _db = db;
            _logger = logger;
        }
    }
}