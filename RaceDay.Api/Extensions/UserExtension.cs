using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class UserExtensions
{
    /// <summary>Reads the logged-in user's id from the session cookie's claims.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}