using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services.UserRepository;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.Commands.Setup;

[CommandContextType(InteractionContextType.Guild,
    InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
[Group("setup", "Setup commands for OpenShock Connection and more")]
public sealed partial class SetupCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly OpenShockDiscordContext _db;
    private readonly ILogger<SetupCommands> _logger;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Default constructor for the SetupCommand
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    /// <param name="userRepository"></param>
    public SetupCommands(OpenShockDiscordContext db, ILogger<SetupCommands> logger, IUserRepository userRepository)
    {
        _db = db;
        _logger = logger;
        _userRepository = userRepository;
    }

    [SlashCommand("shocker", "Configure or refresh your shocker settings")]
    public async Task ShockerConfig()
    {
        await DeferAsync(ephemeral: Context.IsNotDm());

        var user = await _userRepository.GetUser(Context.User.Id);
        if (user == null)
        {
            await FollowupAsync("You need to link your account first.", ephemeral: Context.IsNotDm());
            return;
        }

        var client = user.GetApiClient();
        var shockersAction = await client.GetOwnShockers();
        if (shockersAction.IsT1)
        {
            await FollowupAsync("The authentication token is invalid. Please re-authenticate.");
            return;
        }

        var ownDevices = shockersAction.AsT0.Value;
        var activeShockers = await _db.UsersShockers.Where(x => x.User == Context.User.Id).ToListAsync();
        var ownShockers = ownDevices.SelectMany(x => x.Shockers); 
        
        // cleanup old shockers
        var oldShockers = activeShockers.Where(x => ownShockers.All(y => y.Id != x.ShockerId)).Select(x => x.ShockerId).ToArray();
        if(oldShockers.Length > 0) await _db.UsersShockers.Where(x => oldShockers.Contains(x.ShockerId)).ExecuteDeleteAsync();
        
        var message = Page(0, ownDevices, activeShockers);
        await FollowupAsync(message.Item1, components: message.Item2);
    }

    private (string, MessageComponent) Page(int page, IReadOnlyCollection<ResponseDeviceWithShockers> ownShockers,
        List<UsersShocker> activeShockers)
    {
        var device = ownShockers.ElementAtOrDefault(page);

        if (device == null)
        {
            return ("No devices found", new ComponentBuilder().Build());
        }
        
        var buttonsPage = new ComponentBuilder();
        
        foreach (var chunk
                 in device.Shockers.Chunk(5))
            {
                var row = new ActionRowBuilder();
                foreach (var shocker in chunk)
                {
                    var button = new ButtonBuilder(shocker.Name,
                        $"button_setup_shocker_button@{Context.User.Id}_{shocker.Id}_{page}");
                    button.Style = activeShockers.Any(x => x.ShockerId == shocker.Id)
                        ? ButtonStyle.Success
                        : ButtonStyle.Danger;
                    row.WithButton(button);
                }

                buttonsPage.AddRow(row);
            }
        

        var pageRow = new ActionRowBuilder();
        pageRow.WithButton("Previous", "button_setup_shocker_page@" + (page - 1), ButtonStyle.Secondary, disabled: page < 1);
        pageRow.WithButton("Next", "button_setup_shocker_page@" + (page + 1), ButtonStyle.Secondary, disabled: page >= ownShockers.Count - 1);

        buttonsPage.AddRow(pageRow);

        return ($"Device `{device.Name}` ({page + 1}/{ownShockers.Count})", buttonsPage.Build());
    }

    [ComponentInteraction("button_setup_shocker_button@*.", ignoreGroupNames: true, TreatAsRegex = true)]
    public async Task ShockerButtonInteraction()
    {
        await DeferAsync(ephemeral: Context.IsNotDm());

        var buttonId = ((string)CustomId.GetValue(Context.Interaction.Data)!).Split('@')[1].Split('_');
        if(ulong.Parse(buttonId[0]) != Context.User.Id) return;
        var shockerId = Guid.Parse(buttonId[1]);
        var page = int.Parse(buttonId[2]);
        
        var user = await _userRepository.GetUser(Context.User.Id);
        if (user == null) return;

        var exists = await _db.UsersShockers.AnyAsync(x => x.User == Context.User.Id && x.ShockerId == shockerId);
        var shockerDto = new UsersShocker
        {
            User = Context.User.Id,
            ShockerId = shockerId
        };
        if (exists)
        {
            _db.UsersShockers.Remove(shockerDto);
        }
        else
        {
            _db.UsersShockers.Add(shockerDto); 
        }
        
        await _db.SaveChangesAsync();
        
        var client = user.GetApiClient();
        var shockersActionTask = client.GetOwnShockers();
        var activeShockersTask = _db.UsersShockers.Where(x => x.User == Context.User.Id).ToListAsync();

        await Task.WhenAll(shockersActionTask, activeShockersTask);

        var shockersAction = shockersActionTask.Result;
        var activeShockers = activeShockersTask.Result;

        if (shockersAction.IsT1) return;

        var ownShockers = shockersAction.AsT0.Value;

        var message = Page(page, ownShockers, activeShockers);
        
        await ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = message.Item1;
            properties.Components = message.Item2;
        });
    }

    private static readonly Type DiscordMessageData =
        typeof(BaseDiscordClient).Assembly.GetType("Discord.API.MessageComponentInteractionData")!;

    private static readonly PropertyInfo CustomId =
        DiscordMessageData.GetProperty("CustomId", BindingFlags.Public | BindingFlags.Instance)!;

    [ComponentInteraction("button_setup_shocker_page@*.", ignoreGroupNames: true, TreatAsRegex = true)]
    public async Task ShockerPageButtonInteraction()
    {
        await DeferAsync(ephemeral: Context.IsNotDm());
        var user = await _userRepository.GetUser(Context.User.Id);
        if (user == null) return;

        var client = user.GetApiClient();
        var shockersActionTask = client.GetOwnShockers();
        var activeShockersTask = _db.UsersShockers.Where(x => x.User == Context.User.Id).ToListAsync();

        await Task.WhenAll(shockersActionTask, activeShockersTask);

        var shockersAction = shockersActionTask.Result;
        var activeShockers = activeShockersTask.Result;

        if (shockersAction.IsT1) return;

        var ownShockers = shockersAction.AsT0.Value;
        
        var page = int.Parse(((string)CustomId.GetValue(Context.Interaction.Data)!).Split('@')[1]);

        var message = Page(page, ownShockers, activeShockers);
        
        await ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = message.Item1;
            properties.Components = message.Item2;
        });
    }
}