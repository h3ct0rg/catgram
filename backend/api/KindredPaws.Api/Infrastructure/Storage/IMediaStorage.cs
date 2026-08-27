namespace KindredPaws.Api.Infrastructure.Storage;

public interface IMediaStorage
{
    Task PutAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken);
    Task<string> GetUrlAsync(string objectKey, CancellationToken cancellationToken);
    Task<string> GetUrlAsync(string objectKey, int expirationSeconds, CancellationToken cancellationToken);
}
