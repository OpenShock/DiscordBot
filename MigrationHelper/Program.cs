using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenShock.DiscordBot.OpenShockDiscordDb;

HostBuilder builder = new();
builder.ConfigureServices(collection =>
{
    collection.AddDbContext<OpenShockDiscordContext>();
});

var host = builder.Build();
await host.RunAsync();