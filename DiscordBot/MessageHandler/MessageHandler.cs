using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.SDK.CSharp.Models;
using System.Text.RegularExpressions;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed partial class MessageHandler
{
    private readonly IServiceProvider _serviceProvider;


    public MessageHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [GeneratedRegex("\bbot\b", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private partial Regex BotChannelMatchingRegex();

    private static async Task<bool> CheckUserProfanityShockingOptIn(AsyncServiceScope scope, ulong userDiscordId)
    {
        var db = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
        var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == userDiscordId);

        return user?.ProfanityShocking ?? false;
    }

    private static async Task ShockUserAsync(AsyncServiceScope scope, ulong userDiscordId, float intensityPercent)
    {
        var backendService = scope.ServiceProvider.GetRequiredService<IOpenShockBackendService>();
        await backendService.ControlAllShockers(userDiscordId, (byte)intensityPercent, 1000, ControlType.Shock);
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // Check if the message contains a swear word
        if (ProfanityDetector.TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            float intensityPercent = weight * 100f;

            // If the channel is a bot channel, respond with debug message
            if (BotChannelMatchingRegex().IsMatch(message.Channel.Name))
            {
                await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {intensityPercent}%");
            }

            var authorDiscordId = message.Author.Id;

            await using var scope = _serviceProvider.CreateAsyncScope();

            // Verify user has opted in for profanity shocking
            if (!await CheckUserProfanityShockingOptIn(scope, authorDiscordId))
            {
                return;
            }

            // Send reaction and trigger shock
            await Task.WhenAll([
                    message.AddReactionAsync(new Emoji("⚡")),
                    ShockUserAsync(scope, authorDiscordId, intensityPercent)
                ]);
        }
    }
}