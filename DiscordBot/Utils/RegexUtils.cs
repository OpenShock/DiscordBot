using System.Text.RegularExpressions;

namespace OpenShock.DiscordBot.Utils;

public static class RegexUtils
{
    public static bool IsValidRegexPattern(string pattern)
    {
        try
        {
            _ = Regex.Match("", pattern);
        }
        catch (Exception)
        {
            return false;
        }
        
        return true;
    }
}