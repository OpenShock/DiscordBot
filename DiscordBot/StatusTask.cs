using System.Net.Http.Json;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.Backend.Models;
using OpenShock.SDK.CSharp.Models;
using Timer = System.Timers.Timer;

namespace OpenShock.DiscordBot;

public class StatusTask : IHostedService, IDisposable
{
    private readonly ILogger<StatusTask> _logger;
    private readonly DiscordSocketClient _client;
    private readonly IHttpClientFactory _httpClientFactory;

    private Timer? _timer;

    public StatusTask(ILogger<StatusTask> logger, DiscordSocketClient client, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _client = client;
        _httpClientFactory = httpClientFactory;
    }

    private async void UpdateStatusLoop(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        try
        {
            _logger.LogTrace("Updating status");
            await Update();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while updating status");
        }
    }

    private async Task Update()
    {
        var httpClient = _httpClientFactory.CreateClient("OpenShockBackend");
        var response =
            await httpClient.GetFromJsonAsync<BaseResponse<StatsResponse>>("1/public/stats");
        if (response == null) return;

        await _client.SetActivityAsync(
            new Game($"{response.Data?.DevicesOnline} online devices", ActivityType.Watching));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(TimeSpan.FromSeconds(30));

        _timer.Elapsed += UpdateStatusLoop;

        _timer.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _timer = null;
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}