using OpenShock.DiscordBot.Backend.Models;

namespace OpenShock.DiscordBot.Backend;

public interface IOpenShockApi
{
    public Task<BaseResponse<IEnumerable<ResponseDeviceWithShockers>>> GetOwnShockers(Uri server, string apiKey, CancellationToken ct = default);
}