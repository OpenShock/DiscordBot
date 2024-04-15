using Discord;
using Discord.Interactions;

namespace OpenShock.DiscordBot.Commands;

public class SetupCommand : InteractionModuleBase
{
    [EnabledInDm(true)]
    [SlashCommand("setup", "Start setup for use with OpenShock")]
    public async Task ExecuteSetupCommand()
    {
        await Context.Interaction.RespondWithModalAsync<SetupModal>("setup_menu");
    }
    
    // Responds to the modal.
    [ModalInteraction("setup_menu")]
    public async Task ModalResponce(SetupModal modal)
    {
        
        
        // Build the message to send.
        var message =
            $"cock {Context.User.Mention} penis {modal.ApiKey} ballin {modal.ApiServer}.";

        // Respond to the modal.
        await RespondAsync(message, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
    }
}

public class SetupModal : IModal
{
    public string Title => "OpenShock Connection";
    
    [InputLabel("API Key")]
    [RequiredInput]
    [ModalTextInput("api_key", TextInputStyle.Short, "Enter your API key...", maxLength: 256)]
    public required string ApiKey { get; init; }

    // Additional paremeters can be specified to further customize the input.
    [InputLabel("OpenShock API server")]
    [RequiredInput]
    [ModalTextInput("api_server", TextInputStyle.Short, "https://api.shocklink.net", maxLength: 256, initValue: "https://api.shocklink.net")]
    public required string ApiServer { get; init; }
}