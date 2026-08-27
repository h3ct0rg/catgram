namespace KindredPaws.Api.Infrastructure.Storage;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "kindred-paws-media";
    public bool UseSSL { get; set; }
    public int UrlExpirationSeconds { get; set; } = 3600;
}
