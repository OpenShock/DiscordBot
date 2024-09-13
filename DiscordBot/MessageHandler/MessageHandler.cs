using Discord.WebSocket;

namespace OpenShock.DiscordBot.MessageHandler;

public static class MessageHandler
{
    public static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return;

        // If the channel name does not contain "bot", ignore the message
        if (!message.Channel.Name.Contains("bot", StringComparison.OrdinalIgnoreCase)) return;

        // Check if the message contains a swear word
        if (ProfanityDetector.TryGetProfanityWeight(message.Content, out int count, out float weight))
        {
            // Respond to the message
            await message.Channel.SendMessageAsync($"Profanity detected! {count} bad {(count > 1 ? "words" : "word")}, shocking at {weight * 100f:F0}%");
        }
    }
}
