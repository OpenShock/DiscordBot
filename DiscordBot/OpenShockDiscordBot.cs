using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.DiscordBot.Utils;
using Serilog;

namespace OpenShock.DiscordBot;

public class OpenShockDiscordBot
{
    public static ILogger<OpenShockDiscordBot> Logger = null!;
    
    public static async Task Main(string[] args)
    {
        HostBuilder builder = new();

        builder.UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                if (args is { Length: > 0 }) config.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                        reloadOnChange: false);

                config.AddEnvironmentVariables();
                if (args is { Length: > 0 }) config.AddCommandLine(args);
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            })
            .UseSerilog((context, _, config) => { config.ReadFrom.Configuration(context.Configuration); });
        
        // <---- Services ---->
        
        builder.ConfigureServices((context, services) =>
        {
            var discordBotConfig =  context.Configuration.GetSection("bot").Get<DiscordBotConfig>() ?? throw new Exception("Could not load bot config");
            
            services.AddSingleton(discordBotConfig);

            services.AddDbContextPool<OpenShockDiscordContext>(builder =>
            {
                builder.UseNpgsql(discordBotConfig.Db);
            });
            
            services.AddSingleton(new DiscordSocketClient());
            services.AddSingleton<InteractionService>();
            services.AddSingleton<InteractionHandler>();

            services.AddHostedService<StatusTask>();
        });
        
        try
        {
            var host = builder.Build();
            Logger = host.Services.GetRequiredService<ILogger<OpenShockDiscordBot>>();
            Logger.LogInformation("Starting OpenShock Discord Bot version {Version}",
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
            
            // <---- Initialize Service stuff, this also instantiates the singletons!!! ---->
            
            var client = host.Services.GetRequiredService<DiscordSocketClient>();
            var interactionService = host.Services.GetRequiredService<InteractionService>();
            var interactionHandler = host.Services.GetRequiredService<InteractionHandler>();
            
            client.Log += LoggingUtils.LogAsync;
            client.Ready += () => ReadyAsync(client, interactionService);
            
            interactionService.Log += LoggingUtils.LogAsync;
            
            await interactionHandler.InitializeAsync();
            
            // <---- Run discord client ---->
            
            var config = host.Services.GetRequiredService<DiscordBotConfig>();
            
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
            
            await host.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }

    private static async Task ReadyAsync(BaseSocketClient client, InteractionService interactionService)
    {
        Logger.LogInformation("Connected as [{CurrentUser}]", client.CurrentUser.Username);
        await client.SetActivityAsync(
            new Game($"electricity flow, v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}", ActivityType.Watching));

        await interactionService.RegisterCommandsGloballyAsync();
    }
}