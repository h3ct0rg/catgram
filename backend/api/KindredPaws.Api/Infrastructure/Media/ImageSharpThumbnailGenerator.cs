using KindredPaws.Api.Application.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KindredPaws.Api.Infrastructure.Media;

public sealed class ImageSharpThumbnailGenerator : IThumbnailGenerator
{
    private static readonly string[] SupportedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const int MaxDimension = 320;

    public bool CanGenerate(string contentType) => SupportedContentTypes.Contains(contentType);

    public async Task<(Stream Content, long Length, string ContentType)?> GenerateAsync(Stream source, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(source, cancellationToken);
        image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(MaxDimension, MaxDimension) }));
        var output = new MemoryStream();
        await image.SaveAsync(output, new WebpEncoder(), cancellationToken);
        output.Position = 0;
        return (output, output.Length, "image/webp");
    }
}
