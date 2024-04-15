using Discord;
using Serilog;
using Serilog.Context;

namespace OpenShock.DiscordBot.Utils;

public static class LoggingUtils
{
    public static Task LogAsync(LogMessage log)
    {
        using (LogContext.PushProperty("SourceContext", $"Discord.{log.Source}"))
        {
            switch (log.Severity)
            {
                case LogSeverity.Debug:
                    Log.Verbose(log.Exception, "{Message}", log.Message);
                    break;
                case LogSeverity.Verbose:
                    Log.Debug(log.Exception, "{Message}", log.Message);
                    break;
                case LogSeverity.Info:
                    Log.Information(log.Exception, "{Message}", log.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(log.Exception, "{Message}", log.Message);
                    break;
                case LogSeverity.Error:
                    Log.Error(log.Exception, "{Message}", log.Message);
                    break;
                case LogSeverity.Critical:
                    Log.Fatal(log.Exception, "{Message}", log.Message);
                    break;
                default: // This should never be reached
                    Log.Fatal(log.Exception, "NO TEMPLATE FOUND {Message}", log.Message);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}