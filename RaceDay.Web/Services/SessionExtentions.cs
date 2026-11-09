using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public static class SessionExtensions
{
    private const string UserKey = "CurrentUser";

    public static void SetCurrentUser(this ISession s, SessionUser u) =>
        s.SetString(UserKey, JsonSerializer.Serialize(u));

    public static SessionUser? GetCurrentUser(this ISession s)
    {
        var json = s.GetString(UserKey);
        return json is null ? null : JsonSerializer.Deserialize<SessionUser>(json);
    }
}