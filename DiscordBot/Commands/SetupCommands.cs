using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.SDK.CSharp;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild,
    InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public sealed class SetupCommands : InteractionModuleBase
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<SetupCommand> _logger;

    /// <summary>
    /// Default constructor for the SetupCommand
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    public SetupCommands(OpenShockDiscordContext db, ILogger<SetupCommand> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    [SlashCommand("shocker", "Configure or refresh your shocker settings")]
    public async Task ShockerConfig()
    {
        // var openShockClient = new OpenShockApiClient(new ApiClientOptions()
        // {
        //     Server = 
        // })
        //
        // _db.UsersShockers.Add(new UsersShocker()
        // {
        //     User = 
        // });
    }
}