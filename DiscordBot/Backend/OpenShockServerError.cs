using System.Net;

namespace OpenShock.DiscordBot.Backend;

public readonly struct OpenShockServerError
{
    public required HttpStatusCode StatusCode { get; init; }
    public required string Message { get; init; }
    
}