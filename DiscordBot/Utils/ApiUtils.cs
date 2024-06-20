using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.SDK.CSharp;

namespace OpenShock.DiscordBot.Utils;

public static class ApiUtils
{
    public static IOpenShockApiClient GetApiClient(this User user)
    {
        return new OpenShockApiClient(new ApiClientOptions()
        {
            Token = user.ApiKey,
            Server = new Uri(user.ApiServer)
        });
    }
}