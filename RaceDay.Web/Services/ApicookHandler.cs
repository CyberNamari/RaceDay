namespace RaceDay.Web.Services;

public class ApiCookieHandler : DelegatingHandler
{
    public const string SessionKey = "ApiCookie";
    private const string CookieName = "RaceDay.Session";
    private readonly IHttpContextAccessor _accessor;

    public ApiCookieHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var session = _accessor.HttpContext?.Session;
        var stored = session?.GetString(SessionKey);
        if (!string.IsNullOrEmpty(stored))
            request.Headers.TryAddWithoutValidation("Cookie", $"{CookieName}={stored}");

        var response = await base.SendAsync(request, ct);

        if (session != null && response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var match = cookies.FirstOrDefault(c => c.StartsWith(CookieName + "="));
            if (match != null)
            {
                var value = match.Split(';')[0][(CookieName.Length + 1)..];
                // An expired/cleared cookie comes back empty
                if (string.IsNullOrEmpty(value)) session.Remove(SessionKey);
                else session.SetString(SessionKey, value);
            }
        }
        return response;
    }
}