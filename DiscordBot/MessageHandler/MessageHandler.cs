using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.MessageHandler;

public sealed class MessageHandler
{
    private readonly IServiceProvider _serviceProvider;


    public MessageHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // If the channel name does not contain "bot", ignore the message
        if (!message.Channel.Name.Contains("bot", StringComparison.OrdinalIgnoreCase)) return;

        // Check if the message contains a swear word
        if (ProfanityDetector.TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            var intensity = (byte) (weight * 100f);
            
            // Respond to the message
            await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {intensity}%");
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
            var user = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == message.Author.Id);
            if (user == null) return;
            if(!user.ProfanityShocking) return;
            
            var backendService = scope.ServiceProvider.GetRequiredService<IOpenShockBackendService>();
            await backendService.ControlAllShockers(message.Id, intensity, 1000, ControlType.Shock);
        }
    }
}
