using KindredPaws.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

/// <summary>
/// Streams media objects from MinIO on the client's behalf — MinIO itself is never exposed to the
/// browser (its host/port may not even be reachable from client networks). Two dedicated endpoints serve
/// the original object (/image) and its thumbnail (/thumbnail). See MinioService.
/// </summary>
[ApiController]
[Route("api/v1/media")]
public sealed class MediaController(IMinioService minioService, ILogger<MediaController> logger) : ControllerBase
{
    [HttpGet("image/{**key}")]
    [AllowAnonymous]
    public Task<IActionResult> GetImage(string key, [FromQuery] string? type, CancellationToken cancellationToken) =>
        ServeAsync(key, type, cancellationToken);

    [HttpGet("thumbnail/{**key}")]
    [AllowAnonymous]
    public Task<IActionResult> GetThumbnail(string key, [FromQuery] string? type, CancellationToken cancellationToken) =>
        ServeAsync(key, type, cancellationToken);

    private async Task<IActionResult> ServeAsync(string key, string? type, CancellationToken cancellationToken)
    {
        try
        {
            var content = await minioService.OpenReadAsync(key, cancellationToken);
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(content, string.IsNullOrWhiteSpace(type) ? "application/octet-stream" : type);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read media object '{Key}'.", key);
            return NotFound();
        }
    }
}
