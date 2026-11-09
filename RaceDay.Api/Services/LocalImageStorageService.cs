namespace RaceDay.Api.Services;

public class LocalImageStorageService : IBlobStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    public LocalImageStorageService(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<string> UploadImageAsync(string folder, IFormFile file, CancellationToken ct = default)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", folder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using (var fs = File.Create(Path.Combine(dir, name)))
        {
            await file.CopyToAsync(fs, ct);
        }

        var req = _http.HttpContext!.Request;
        return $"{req.Scheme}://{req.Host}/uploads/{folder}/{name}";
    }
}