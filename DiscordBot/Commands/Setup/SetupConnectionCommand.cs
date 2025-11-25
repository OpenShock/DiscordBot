using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.Commands.Setup;

public sealed partial class SetupCommands
{
    private const string SetupModalId = "setup_menu";
    
    [SlashCommand("connection", "Start setup for use with OpenShock")]
    public async Task ExecuteSetupCommand()
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id);

        // First time setup
        if (user == null)
        {
            await Context.Interaction.RespondWithModalAsync<SetupModal>(SetupModalId);
            return;
        }
        
        // Fill in previous data
        var previousData = new SetupModal
        {
            ApiToken = user.ApiKey,
            ApiServer = user.ApiServer
        };
        await Context.Interaction.RespondWithModalAsync(SetupModalId, previousData);
    }
    
    // Responds to the modal.
    [ModalInteraction(SetupModalId, ignoreGroupNames: true)]
    public async Task ModalResponce(SetupModal modal)
    {
        await DeferAsync(ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
        
        if (!Uri.TryCreate(modal.ApiServer, UriKind.Absolute, out var serverUri))
        {
            await FollowupAsync("Invalid API Server URL", ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
            return;
        }
        
        var openShock = new OpenShockApiClient(new ApiClientOptions
        {
            Server = serverUri,
            Token = modal.ApiToken
        });
        
        // Test the connection
        try
        {
            var rootResponse = await openShock.GetRoot();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while testing OpenShock connection for {server}", serverUri.ToString());
            await FollowupAsync("Error while testing OpenShock connection", ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
            return;
        }

        SelfResponse selfResponse;
        
        // Get user details
        try
        {
            var openShockUser = await openShock.GetSelf();

            if (openShockUser.IsT1)
            {
                await FollowupAsync(
                    "Authentication at the OpenShock Server failed, are you sure you entered the correct API Token?",
                    ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
                return;
            }

            selfResponse = openShockUser.AsT0.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting user details for user {server}", serverUri.ToString());
            await FollowupAsync("Error while getting user details from OpenShock Server", ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
            return;
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id);
        
        if (user == null)
        {
            var newUser = new User
            {
                DiscordId = Context.User.Id,
                OpenshockId = selfResponse.Id,
                ApiKey = modal.ApiToken,
                ApiServer = serverUri.ToString()
            };
            
            _db.Users.Add(newUser);
        }
        else
        {
            user.ApiServer = serverUri.ToString();
            user.ApiKey = modal.ApiToken;
            user.OpenshockId = selfResponse.Id;
        }
        
        await _db.SaveChangesAsync();
        
        // Respond to the modal
        await FollowupAsync($"Successfully logged in as {selfResponse.Name}! 🎉", ephemeral: Context.Interaction.ContextType != InteractionContextType.BotDm);
    }
}

public sealed class SetupModal : IModal
{
    public string Title => "OpenShock Server Connection";
    
    [InputLabel("API Token")]
    [RequiredInput]
    [ModalTextInput("api_token", TextInputStyle.Short, "Enter your API Token...", maxLength: 256)]
    public required string ApiToken { get; init; }
    
    [InputLabel("OpenShock API server")]
    [RequiredInput]
    [ModalTextInput("api_server", TextInputStyle.Short, "https://api.openshock.app", maxLength: 256, initValue: "https://api.openshock.app")]
    public required string ApiServer { get; init; }
}