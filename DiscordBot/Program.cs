﻿using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenShock.DiscordBot;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Services;
using OpenShock.DiscordBot.Services.UserRepository;
using OpenShock.DiscordBot.Utils;
using Serilog;
using System.Reflection;
using OpenShock.DiscordBot.Services.ProfanityDetector;

try
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
            .AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: false);

        config.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        config.AddEnvironmentVariables();
        if (args is { Length: > 0 }) config.AddCommandLine(args);
    })
    .UseDefaultServiceProvider((context, options) =>
    {
        var isDevelopment = context.HostingEnvironment.IsDevelopment();
        options.ValidateScopes = isDevelopment;
        options.ValidateOnBuild = isDevelopment;
    })
    .UseSerilog((context, _, config) => config.ReadFrom.Configuration(context.Configuration));

// <---- Services ---->

builder.ConfigureServices((context, services) =>
{
    var botConfig = context.Configuration.GetSection("bot");
    var discordBotConfig = botConfig.Get<DiscordBotConfig>() ??
                           throw new Exception("Could not load bot config");

    services.AddSingleton(discordBotConfig);

    services.AddDbContextPool<OpenShockDiscordContext>(optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(discordBotConfig.Db.Conn);

        // ReSharper disable once InvertIf
        if (discordBotConfig.Db.Debug)
        {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }
    });

    services.AddPooledDbContextFactory<OpenShockDiscordContext>(optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(discordBotConfig.Db.Conn);
        
        // ReSharper disable once InvertIf
        if (discordBotConfig.Db.Debug)
        {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }
    });

    services.AddSingleton<IUserRepository, UserRepository>();
    services.AddSingleton<IProfanityDetector, ProfanityDetector>();
    services.AddSingleton<IOpenShockBackendService, OpenShockBackendService>();
    services.AddSingleton<MessageHandler>();

    services.AddSingleton<DiscordSocketClient>(new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged |
                         GatewayIntents.MessageContent
    }));
    services.AddSingleton<InteractionService>(x =>
        new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
    services.AddSingleton<InteractionHandler>();

    services.AddHttpClient("OpenShockBackend", httpClient =>
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenShockDiscordBot/1.0");
        httpClient.BaseAddress = new Uri("https://api.openshock.app/");
    });
    services.AddHostedService<StatusTask>();
});


    var host = builder.Build();

    Log.Information("Starting OpenShock Discord Bot version {Version}", Assembly.GetEntryAssembly()?.GetName().Version?.ToString());

    var config = host.Services.GetRequiredService<DiscordBotConfig>();

    // <---- DATABASE MIGRATION ---->

    if (!config.Db.SkipMigration)
    {
        Log.Information("Running database migrations...");

        using var scope = host.Services.CreateScope();
        var openShockContext = scope.ServiceProvider.GetRequiredService<OpenShockDiscordContext>();
        var pendingMigrations = (await openShockContext.Database.GetPendingMigrationsAsync()).ToList();

        if (pendingMigrations.Count > 0)
        {
            Log.Information("Found pending migrations, applying [{@Migrations}]", pendingMigrations);
            await openShockContext.Database.MigrateAsync();
            Log.Information("Applied database migrations... proceeding with startup");
        }
        else
        {
            Log.Information("No pending migrations found, proceeding with startup");
        }
    }
    else
    {
        Log.Warning("Skipping possible database migrations...");
    }

    // <---- Initialize Service stuff, this also instantiates the singletons!!! ---->

    await host.Services.GetRequiredService<IProfanityDetector>().LoadProfanityRulesAsync();

    var client = host.Services.GetRequiredService<DiscordSocketClient>();
    var interactionService = host.Services.GetRequiredService<InteractionService>();
    var interactionHandler = host.Services.GetRequiredService<InteractionHandler>();
    var messageHandler = host.Services.GetRequiredService<MessageHandler>();

    client.Log += LoggingUtils.LogAsync;
    client.Ready += () => ReadyAsync(client, interactionService);

    client.MessageReceived += messageHandler.HandleMessageAsync;

    interactionService.Log += LoggingUtils.LogAsync;

    await interactionHandler.InitializeAsync();

    // <---- Run discord client ---->

    await client.LoginAsync(TokenType.Bot, config.Token);
    await client.StartAsync();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
finally
{
    Log.CloseAndFlush();
}

static async Task ReadyAsync(BaseSocketClient client, InteractionService interactionService)
{
    Log.Information("Connected as [{CurrentUser}]", client.CurrentUser.Username);
    await client.SetActivityAsync(
        new Game($"electricity flow, v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString()}",
            ActivityType.Watching));

    await interactionService.RegisterCommandsGloballyAsync();
}