using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Errors;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.Commands;

[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public sealed class ControlCommands : InteractionModuleBase
{
    private readonly OpenShockDiscordContext _db;

    public ControlCommands(OpenShockDiscordContext db)
    {
        _db = db;
    }

    [SlashCommand("shock", "Shock a friend that has whitelisted you before")]
    public async Task ShockCommand(SocketUser user, [Range(1, 100)] byte intensity = 50,
        [Range(0.3, 30)] float duration = 5, ShockMode mode = ShockMode.Random)
    {
        if(intensity is < 1 or > 100)
        {
            await FollowupAsync("Intensity must be at least 1% and at most 100%");
            return;
        }
        
        if(duration is < 0.3f or > 30f)
        {
            await FollowupAsync("Duration must be at least 0.3s and at most 30s");
            return;
        }
        
        await DeferAsync();
        User shockUser;
        if (Context.User.Id == user.Id)
        {
            var ourUser = await _db.Users.FirstOrDefaultAsync(x => x.DiscordId == Context.User.Id);
            if (ourUser == null)
            {
                await FollowupAsync("You need to link your account first.");
                return;
            }

            shockUser = ourUser;
        }
        else
        {
            var friendUser = await _db.UsersFriendwhitelists
                .Where(x => x.WhitelistedFriend == Context.User.Id && x.User == user.Id).Select(x => x.UserNavigation)
                .FirstOrDefaultAsync();
            if (friendUser == null)
            {
                await FollowupAsync("You can only shock friends that have whitelisted you before.");
                return;
            }

            shockUser = friendUser;
        }

        // Shock the user
        var shocker = await _db.UsersShockers.Where(x => x.User == user.Id).ToListAsync();

        if (shocker.Count < 1)
        {
            await FollowupAsync("The user has no shockers configured.");
            return;
        }

        var client = shockUser.GetApiClient();

        Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission, UnauthenticatedError>>
            control;

        var durationMs = (ushort)(duration * 1000);

        if (mode == ShockMode.Random)
        {
            var randomItemIndex = Random.Shared.Next(shocker.Count);
            var randomShocker = shocker[randomItemIndex];

            // Shock the user
            control = client.ControlShocker(new ControlRequest
            {
                CustomName = $"{Context.User.GlobalName} [Discord]",
                Shocks =
                [
                    new Control
                    {
                        Id = randomShocker.ShockerId,
                        Duration = durationMs,
                        Intensity = intensity,
                        Type = ControlType.Shock
                    }
                ]
            });
        }
        else
        {
            var shocks = shocker.Select(x => new Control
            {
                Id = x.ShockerId,
                Duration = durationMs,
                Intensity = intensity,
                Type = ControlType.Shock
            });

            control = client.ControlShocker(new ControlRequest
            {
                CustomName = $"{Context.User.GlobalName} [Discord]",
                Shocks = shocks
            });
        }

        var inSeconds = MathF.Round(durationMs / 1000f, 1).ToString(CultureInfo.InvariantCulture);

        var controlResponse = await control;
        await controlResponse.Match<Task>(
            success => FollowupAsync($"Shocking :zap: {user.Mention} at {intensity}% for {inSeconds}s",
                allowedMentions: AllowedMentions.All),
            notFound => Task.WhenAll(FollowupAsync("The user's shocker was not found."),
                user.SendMessageAsync(
                    $"You were shocked by {Context.User.Mention} but shocker with id `{notFound.Value}` was not found. Try running the `/setup shockers` command again.")),
            paused => FollowupAsync(
                $"The user's shocker is paused. Ask them to unpause it if you want to use it. ||Shocker ID: `{paused.Value}`||"),
            noPermission => FollowupAsync(
                $"Server indicated no permissions for the shocker, this is likely a bug and should not happen. Feel free to contact support. ||Shocker ID: `{noPermission.Value}`||"),
            unauthenticated => Task.WhenAll(FollowupAsync(
                    $"The OpenShock Server Connection is not authenticated. Please wait for {user.Mention} to run the `/setup connection` command to re authenticate."),
                user.SendMessageAsync(
                    $"You were shocked by {Context.User.Mention} but the OpenShock Server Connection is not authenticated. Please run the `/setup connection` command to re authenticate.")));
    }

    public enum ShockMode
    {
        Random = 0,
        All = 1
    }
}