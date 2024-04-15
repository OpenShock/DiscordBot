using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using OpenShock.DiscordBot.Backend.Models;

namespace OpenShock.DiscordBot.Backend;

public class OpenShockApi : IOpenShockApi
{
    private static readonly HttpClient HttpClient = new();
    private readonly DiscordSocketClient _client;
    private readonly ILogger<OpenShockApi> _logger;

    public OpenShockApi(DiscordSocketClient client, ILogger<OpenShockApi> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<BaseResponse<IEnumerable<ResponseDeviceWithShockers>>> GetOwnShockers(Uri server, string apiKey,
        CancellationToken ct = default)
    {
        var response =
            await Request<IEnumerable<ResponseDeviceWithShockers>, object>(server, apiKey, "/1/shockers/own",
                HttpMethod.Get, null, ct);

        return new BaseResponse<IEnumerable<ResponseDeviceWithShockers>>();
    }

    private async Task<OneOf<BaseResponse<T>, OpenShockServerError, Error<string>>> Request<T, T1>(Uri server,
        string apiKey,
        string path, HttpMethod method, T1? requestBody, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(method, new Uri(server, path));
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, MediaTypeNames.Application.Json);
        request.Headers.Add("OpenShockToken", apiKey);

        var response = await HttpClient.SendAsync(request, ct);
        var json = await JsonSerializer.DeserializeAsync<BaseResponse<T>>(await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);
        if (json == null)
        {
            _logger.LogError("Couldn't deserialize response body from openshock backend");
            return new Error<string>("OpenShockApi::GetFromApi|json=null");
        }

        return json;
    }
}