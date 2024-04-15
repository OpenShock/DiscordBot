using Discord.Interactions;

namespace OpenShock.DiscordBot.Commands;

public class TestCommand : InteractionModuleBase
{

    [SlashCommand("test", "test")]
    public async Task Execute()
    {
        await RespondAsync("Test");
        Context.Interaction.RespondAsync();
    }
    
}