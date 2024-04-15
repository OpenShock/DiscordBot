using Discord;

namespace OpenShock.DiscordBot.Utils;

public static class Embeds
{
    public static Embed SlashCommandConstructionFailed()
    {
        EmbedBuilder internalError = new()
        {
            Title = "Internal Error",
            Color = Color.Red
        };
        internalError.AddField("Error", "Error while constructing slash command class");
        return internalError.Build();
    }

    public static Embed SlashCommandError(string ex)
    {
        EmbedBuilder internalError = new()
        {
            Title = "Error",
            Description = ex,
            Color = Color.Red
        };
        return internalError.Build();
    }

    public static Embed CommandNotFound()
    {
        EmbedBuilder notFound = new()
        {
            Title = "Command could not be found",
            Color = Color.Red
        };
        return notFound.Build();
    }

    private static string UppercaseFirst(this string s) =>
        string.IsNullOrEmpty(s) ? string.Empty : $"{char.ToUpper(s[0])}{s[1..]}";
}