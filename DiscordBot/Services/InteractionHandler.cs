using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.Utils;

namespace OpenShock.DiscordBot.Services;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly ILogger<InteractionHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InteractionHandler(DiscordSocketClient client, IServiceProvider serviceProvider,
        InteractionService interactionService, ILogger<InteractionHandler> logger)
    {
        _client = client;
        _serviceProvider = serviceProvider;
        _interactionService = interactionService;
        _logger = logger;

        _client.InteractionCreated += HandleInteraction;
        _interactionService.SlashCommandExecuted += InteractionServiceOnSlashCommandExecuted;
    }

    private async Task InteractionServiceOnSlashCommandExecuted(SlashCommandInfo info, IInteractionContext ctx,
        IResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Error while executing command [{Command}] for user [{User}]({UserId}) with reason [{ErrorReason}]",
                info.Name, ctx.User,
                ctx.User.Id, result.ErrorReason);

            var embed = Embeds.SlashCommandError(string.IsNullOrEmpty(result.ErrorReason) ? "No error found" : result.ErrorReason);
            
            if (!ctx.Interaction.HasResponded)
            {
                await ctx.Interaction.RespondAsync(embed: embed, ephemeral: true);
                return;
            }

            var op = await ctx.Interaction.GetOriginalResponseAsync();

            if (op.Flags != null && op.Flags.Value.HasFlag(MessageFlags.Loading))
            {
                await ctx.Interaction.FollowupAsync(embed: embed);
                return;
            }
            await op.ModifyAsync(properties =>
            {
                properties.Embed = embed;
                properties.Content = string.Empty;
            });

            return;
        }

        _logger.LogInformation("Executed command [{Command}] for user [{User}]({UserId})", info.Name, ctx.User,
            ctx.User.Id);
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(GetType().Assembly, _serviceProvider);
    }

    # region Execution

    private async Task HandleInteraction(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(_client, arg);
        await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
    }

    # endregion
}