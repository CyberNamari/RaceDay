using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(string folder, IFormFile file, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;
    public BlobStorageService(IConfiguration config) => _config = config;

    public async Task<string> UploadImageAsync(string folder, IFormFile file, CancellationToken ct = default)
    {
        var connection = _config["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var containerName = _config["AzureStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connection, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, ct);

        return blob.Uri.ToString();
    }
}