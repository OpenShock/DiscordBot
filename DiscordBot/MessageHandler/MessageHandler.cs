using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.DiscordBot.Services.ProfanityDetector;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot;

public sealed partial class MessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProfanityDetector _profanityDetector;


    public MessageHandler(IServiceProvider serviceProvider, IProfanityDetector profanityDetector)
    {
        _serviceProvider = serviceProvider;
        _profanityDetector = profanityDetector;
    }

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

        bool isInBotChannel = message.Channel.Id == 1114123393567047730;

        if (isInBotChannel && message.Content.StartsWith("debug "))
        {
            await message.Channel.SendMessageAsync($"Message received: ``{message.Content[6..]}``");
        }

        // Check if message starts with the lightning bolt emoji and mentions a user
        if (message.Content.StartsWith('⚡') && message.MentionedUsers.Count > 0)
        {
            // Checkmark the message
            await message.AddReactionAsync(new Emoji("✅"));
            return;
        }

        // Check if the message contains a swear word
        if (_profanityDetector.TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            float intensityPercent = MathF.Min(weight, 1f) * 100f;

            var authorDiscordId = message.Author.Id;

            await using var scope = _serviceProvider.CreateAsyncScope();

            // Verify user has opted in for profanity shocking
            if (!await CheckUserProfanityShockingOptIn(scope, authorDiscordId))
            {
                // If the channel is a bot channel, respond with debug message
                if (isInBotChannel) await message.Channel.SendMessageAsync("Profanity detected, but cant shock you, register and/or enable it");
                return;
            }

            // If the channel is a bot channel, respond with debug message
            if (isInBotChannel) await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {intensityPercent:F0}%");

            // Send reaction and trigger shock
            await Task.WhenAll([
                    message.AddReactionAsync(new Emoji("⚡")),
                    ShockUserAsync(scope, authorDiscordId, intensityPercent)
                ]);
        }
    }
}
