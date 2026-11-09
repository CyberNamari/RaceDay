using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public HttpStatusCode? Status { get; init; }
    public bool Unauthorized => Status == HttpStatusCode.Unauthorized;
}

public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public ApiClient(IHttpClientFactory factory) => _http = factory.CreateClient("RaceDayApi");

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(() => _http.GetAsync(url));
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(() => _http.PostAsJsonAsync(url, body ?? new { }, Json));
    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(() => _http.PutAsJsonAsync(url, body, Json));
    public Task<ApiResult<object>> DeleteAsync(string url) =>
        SendAsync<object>(() => _http.DeleteAsync(url));

    public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file) =>
        SendAsync<T>(async () =>
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var part = new StreamContent(stream);
            part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(part, "file", file.FileName);
            return await _http.PostAsync(url, content);
        });

    private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> call)
    {
        try
        {
            using var response = await call();
            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (response.Content.Headers.ContentLength != 0 && typeof(T) != typeof(object))
                    data = await response.Content.ReadFromJsonAsync<T>(Json);
                return new ApiResult<T> { Success = true, Data = data, Status = response.StatusCode };
            }
            return new ApiResult<T>
            {
                Success = false,
                Status = response.StatusCode,
                Error = await ReadError(response)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ApiResult<T>
            {
                Success = false,
                Error = "We couldn't reach the RaceDay service. Please try again shortly."
            };
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        try
        {
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var msg)) return msg.GetString() ?? "Something went wrong.";
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                return string.Join(" ", errs.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(e => e.GetString())));
        }
        catch { /* fall through */ }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}