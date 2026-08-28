using KindredPaws.Api.Application.Shared;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace KindredPaws.Api.Infrastructure.Storage;

public sealed record StoredMediaResult(string ObjectKey, string? ThumbnailKey, string ContentType, long SizeBytes);

public interface IMinioService
{
    /// <summary>Validates the upload, stores the original object and (for images) a generated thumbnail in
    /// MinIO. Returns the resulting object keys. Everything MinIO-related lives here so domain services
    /// never talk to the storage backend directly.</summary>
    Task<StoredMediaResult> StoreAsync(string keyPrefix, string fileName, string contentType, long length, Stream content, bool withThumbnail, CancellationToken cancellationToken);

    /// <summary>Builds the URL the browser will use to fetch the original object through the media-proxy
    /// endpoint (/api/v1/media/image/...). contentType is the one already validated/stored at upload time
    /// (not re-derived from MinIO metadata, which doesn't reliably round-trip and can make the proxy
    /// respond with an ambiguous/missing Content-Type — enough for Chrome's ORB to block the cross-origin
    /// image load).</summary>
    Task<string> GetImageUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken);

    /// <summary>Builds the URL to the thumbnail proxy endpoint (/api/v1/media/thumbnail/...). Thumbnails are
    /// always WebP. Returns null when the object has no thumbnail.</summary>
    Task<string?> GetThumbnailUrlAsync(string? thumbnailKey, CancellationToken cancellationToken);

    /// <summary>Reads an object's raw bytes for the media-proxy endpoint to relay to the browser — the
    /// storage backend (MinIO) is never reached by the client directly.</summary>
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
}

/// <summary>
/// Single place that encapsulates everything MinIO: object storage (original + thumbnail), URL building for
/// the media-proxy endpoints and read-back for streaming. MinIO is purely an internal storage container —
/// the browser never talks to it directly; all URLs point back at this API's own /api/v1/media endpoints,
/// which stream objects server-side. This avoids requiring MinIO's port to be reachable from client
/// networks/browsers at all, and sidesteps presigned-URL expiry entirely.
/// </summary>
public sealed class MinioService : IMinioService
{
    private const long MaxMediaBytes = 50 * 1024 * 1024;
    // "image/jpg" isn't a registered MIME type but some browsers/OS combinations report it anyway for
    // .jpg files instead of the standard "image/jpeg" — accept both rather than silently rejecting them.
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp", "video/mp4"];

    private readonly IMinioClient client;
    private readonly MinioOptions options;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IThumbnailGenerator thumbnailGenerator;
    private readonly ILogger<MinioService> logger;

    public MinioService(IOptions<MinioOptions> options, IHttpContextAccessor httpContextAccessor, IThumbnailGenerator thumbnailGenerator, ILogger<MinioService> logger)
    {
        this.options = options.Value;
        this.httpContextAccessor = httpContextAccessor;
        this.thumbnailGenerator = thumbnailGenerator;
        this.logger = logger;
        client = new MinioClient().WithEndpoint(this.options.Endpoint).WithCredentials(this.options.AccessKey, this.options.SecretKey).WithSSL(this.options.UseSSL).Build();
    }

    public async Task<StoredMediaResult> StoreAsync(string keyPrefix, string fileName, string contentType, long length, Stream content, bool withThumbnail, CancellationToken ct)
    {
        if (length <= 0 || length > MaxMediaBytes) throw new ArgumentException("El archivo excede 50 MB.");
        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"Tipo de archivo no permitido: '{contentType}'. Usa JPG, PNG, WEBP o MP4.");

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var key = $"{keyPrefix}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        buffer.Position = 0;
        await PutAsync(key, buffer, length, contentType, ct);

        var thumbnailKey = withThumbnail ? await TryGenerateThumbnailAsync(buffer, contentType, keyPrefix, ct) : null;
        return new StoredMediaResult(key, thumbnailKey, contentType, length);
    }

    private async Task<string?> TryGenerateThumbnailAsync(MemoryStream buffer, string contentType, string keyPrefix, CancellationToken ct)
    {
        if (!thumbnailGenerator.CanGenerate(contentType)) return null;
        try
        {
            buffer.Position = 0;
            var thumbnail = await thumbnailGenerator.GenerateAsync(buffer, ct);
            if (thumbnail is null) return null;
            var thumbnailKey = $"{keyPrefix}/{Guid.NewGuid():N}-thumb.webp";
            await PutAsync(thumbnailKey, thumbnail.Value.Content, thumbnail.Value.Length, thumbnail.Value.ContentType, ct);
            await thumbnail.Value.Content.DisposeAsync();
            return thumbnailKey;
        }
        catch (Exception ex)
        {
            // Thumbnails are a nice-to-have — a decoding/storage failure here must never block the
            // primary media upload itself.
            logger.LogWarning(ex, "Could not generate a thumbnail for media '{KeyPrefix}' ({ContentType}); continuing without one.", keyPrefix, contentType);
            return null;
        }
    }

    private async Task PutAsync(string objectKey, Stream content, long length, string contentType, CancellationToken ct)
    {
        var bucket = options.BucketName;
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists) await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
        await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(objectKey).WithStreamData(content).WithObjectSize(length).WithContentType(contentType), ct);
    }

    public Task<string> GetImageUrlAsync(string objectKey, string contentType, CancellationToken ct) => Task.FromResult(BuildUrl($"image/{EscapeKey(objectKey)}", contentType));
    public Task<string?> GetThumbnailUrlAsync(string? thumbnailKey, CancellationToken ct) => Task.FromResult(thumbnailKey is null ? null : BuildUrl($"thumbnail/{EscapeKey(thumbnailKey)}", "image/webp"));

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await client.GetObjectAsync(new GetObjectArgs().WithBucket(options.BucketName).WithObject(objectKey).WithCallbackStream((stream, token) => stream.CopyToAsync(buffer, token)), ct);
        buffer.Position = 0;
        return buffer;
    }

    private string BuildUrl(string path, string contentType)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        var query = $"?type={Uri.EscapeDataString(contentType)}";
        return request is null ? $"/api/v1/media/{path}{query}" : $"{request.Scheme}://{request.Host}/api/v1/media/{path}{query}";
    }

    private static string EscapeKey(string objectKey) => string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString));
}
