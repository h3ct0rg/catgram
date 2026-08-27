using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace KindredPaws.Api.Infrastructure.Storage;

public sealed class MinioMediaStorage(IOptions<MinioOptions> options) : IMediaStorage
{
    private readonly IMinioClient client = new MinioClient().WithEndpoint(options.Value.Endpoint).WithCredentials(options.Value.AccessKey, options.Value.SecretKey).WithSSL(options.Value.UseSSL).Build();

    public async Task PutAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken)
    {
        var bucket = options.Value.BucketName;
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!exists) await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        await client.PutObjectAsync(new PutObjectArgs().WithBucket(bucket).WithObject(objectKey).WithStreamData(content).WithObjectSize(length).WithContentType(contentType), cancellationToken);
    }

    public Task<string> GetUrlAsync(string objectKey, CancellationToken cancellationToken) => GetUrlAsync(objectKey, options.Value.UrlExpirationSeconds, cancellationToken);

    public Task<string> GetUrlAsync(string objectKey, int expirationSeconds, CancellationToken cancellationToken) =>
        client.PresignedGetObjectAsync(new PresignedGetObjectArgs().WithBucket(options.Value.BucketName).WithObject(objectKey).WithExpiry(expirationSeconds));
}
