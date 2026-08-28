namespace KindredPaws.Api.Infrastructure.Storage;

public interface IMediaStorage
{
    Task PutAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken);

    /// <summary>Builds the URL the browser will use to fetch this object through the media-proxy
    /// endpoint. contentType is the one already validated/stored at upload time (not re-derived from
    /// MinIO metadata, which doesn't reliably round-trip and can make the proxy respond with an
    /// ambiguous/missing Content-Type — enough for Chrome's ORB to block the cross-origin image load).</summary>
    Task<string> GetUrlAsync(string objectKey, string contentType, CancellationToken cancellationToken);

    /// <summary>Reads an object's raw bytes for the media-proxy endpoint to relay to the browser — the
    /// storage backend (MinIO) is never reached by the client directly.</summary>
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
}
