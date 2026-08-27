namespace KindredPaws.Api.Application.Shared;

public interface IThumbnailGenerator
{
    bool CanGenerate(string contentType);
    Task<(Stream Content, long Length, string ContentType)?> GenerateAsync(Stream source, CancellationToken cancellationToken);
}
