using System.Security.Claims;

namespace OpenShock.Activity.Api.Auth;

public static class ClaimsExtensions
{
    /// <summary>The Discord user id, taken from the JWT <c>sub</c> claim.</summary>
    public static ulong GetDiscordId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub")
                  ?? throw new InvalidOperationException("JWT is missing the 'sub' claim.");
        return ulong.Parse(sub);
    }

    /// <summary>The Discord display name, taken from the JWT <c>name</c> claim.</summary>
    public static string GetDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue("name") ?? principal.GetDiscordId().ToString();
}
