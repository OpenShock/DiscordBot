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

    private async Task SendShock(ulong discordId, float intensityPercent)
    {
        var intensity = (byte)intensityPercent;

        await using var scope = _serviceProvider.CreateAsyncScope();
        
        var db = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
        var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == discordId);
        if (user == null) return;
        if (!user.ProfanityShocking) return;

        var backendService = scope.ServiceProvider.GetRequiredService<IOpenShockBackendService>();
        await backendService.ControlAllShockers(discordId, intensity, 1000, ControlType.Shock);
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // Check if the message contains a swear word
        if (ProfanityDetector.TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            float intensityPercent = weight * 100f;

            List<Task> tasks = [];

            // If the channel is a bot channel, respond with debug message
            if (BotChannelMatchingRegex().IsMatch(message.Channel.Name))
            {
                tasks.Add(message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {intensityPercent}%"));
            }

            // Trigger the shock
            tasks.Add(SendShock(message.Author.Id, intensityPercent));

            // Add shock emoji on complete
            tasks.Add(message.AddReactionAsync(new Emoji("⚡")));

            await Task.WhenAll(tasks);
        }
    }
}