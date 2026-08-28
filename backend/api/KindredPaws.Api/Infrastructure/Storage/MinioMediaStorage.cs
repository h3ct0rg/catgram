using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace KindredPaws.Api.Infrastructure.Storage;

/// <summary>
/// MinIO is purely an internal storage container — the browser never talks to it directly. GetUrlAsync
/// returns an absolute URL pointing back at this API's own /api/v1/media endpoint (see MediaController),
/// which streams the object server-side. This avoids requiring MinIO's port to be reachable from client
/// networks/browsers at all, and sidesteps presigned-URL expiry entirely.
/// </summary>
public sealed class MinioMediaStorage(IOptions<MinioOptions> options, IHttpContextAccessor httpContextAccessor) : IMediaStorage
{
    private readonly IMinioClient client = new MinioClient().WithEndpoint(options.Value.Endpoint).WithCredentials(options.Value.AccessKey, options.Value.SecretKey).WithSSL(options.Value.UseSSL).Build();

    public async Task PutAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken)
    {
        var bucket = options.Value.BucketName;
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!exists) await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(objectKey).WithStreamData(content).WithObjectSize(length).WithContentType(contentType), cancellationToken);
    }

    public Task<string> GetUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken)
    {
        var encodedKey = string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString));
        var query = $"?type={Uri.EscapeDataString(contentType)}";
        var request = httpContextAccessor.HttpContext?.Request;
        var url = request is null ? $"/api/v1/media/{encodedKey}{query}" : $"{request.Scheme}://{request.Host}/api/v1/media/{encodedKey}{query}";
        return Task.FromResult(url);
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await client.GetObjectAsync(new GetObjectArgs().WithBucket(options.Value.BucketName).WithObject(objectKey).WithCallbackStream((stream, ct) => stream.CopyToAsync(buffer, ct)), cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}
