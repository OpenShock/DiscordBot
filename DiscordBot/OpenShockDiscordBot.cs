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
using OpenShock.DiscordBot.Services.UserRepository;
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
            var discordBotConfig = context.Configuration.GetSection("bot").Get<DiscordBotConfig>() ??
                                   throw new Exception("Could not load bot config");

            services.AddSingleton(discordBotConfig);

            services.AddDbContextPool<OpenShockDiscordContext>(x =>
            {
                x.UseNpgsql(discordBotConfig.Db.Conn);
                
                // ReSharper disable once InvertIf
                if (discordBotConfig.Db.Debug)
                {
                    x.EnableDetailedErrors();
                    x.EnableSensitiveDataLogging();
                }
            });

            services.AddSingleton<IUserRepository, UserRepository>();
            
            services.AddSingleton(new DiscordSocketClient());
            services.AddSingleton<InteractionService>(x =>
                new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
            services.AddSingleton<InteractionHandler>();

            services.AddHostedService<StatusTask>();
        });

        try
        {
            var host = builder.Build();
            Logger = host.Services.GetRequiredService<ILogger<OpenShockDiscordBot>>();
            Logger.LogInformation("Starting OpenShock Discord Bot version {Version}",
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString());

            var config = host.Services.GetRequiredService<DiscordBotConfig>();
            
            // <---- DATABASE MIGRATION ---->
            
            if (!config.Db.SkipMigration)
            {
                Logger.LogInformation("Running database migrations...");
                using var scope = host.Services.CreateScope();
                var openShockContext = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
                var pendingMigrations = (await openShockContext.Database.GetPendingMigrationsAsync()).ToList();

                if (pendingMigrations.Count > 0)
                {
                    Logger.LogInformation("Found pending migrations, applying [{@Migrations}]", pendingMigrations);
                    await openShockContext.Database.MigrateAsync();
                    Logger.LogInformation("Applied database migrations... proceeding with startup");
                }
                else Logger.LogInformation("No pending migrations found, proceeding with startup");
            }
            else Logger.LogWarning("Skipping possible database migrations...");
            
            // <---- Initialize Service stuff, this also instantiates the singletons!!! ---->

            var client = host.Services.GetRequiredService<DiscordSocketClient>();
            var interactionService = host.Services.GetRequiredService<InteractionService>();
            var interactionHandler = host.Services.GetRequiredService<InteractionHandler>();

            client.Log += LoggingUtils.LogAsync;
            client.Ready += () => ReadyAsync(client, interactionService);

            interactionService.Log += LoggingUtils.LogAsync;

            await interactionHandler.InitializeAsync();

            // <---- Run discord client ---->

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
            new Game($"electricity flow, v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}",
                ActivityType.Watching));
        
        await interactionService.RegisterCommandsGloballyAsync();
    }
}