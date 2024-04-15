namespace OpenShock.DiscordBot.Backend.Models;

public class ResponseDeviceWithShockers : ResponseDevice
{
    public required IEnumerable<ShockerResponse> Shockers { get; set; }
}