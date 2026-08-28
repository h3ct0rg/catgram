using KindredPaws.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

/// <summary>
/// Streams media objects from MinIO on the client's behalf — MinIO itself is never exposed to the
/// browser (its host/port may not even be reachable from client networks). See MinioMediaStorage.
/// </summary>
[ApiController]
[Route("api/v1/media")]
public sealed class MediaController(IMediaStorage mediaStorage, ILogger<MediaController> logger) : ControllerBase
{
    [HttpGet("{**key}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string key, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        try
        {
            var content = await mediaStorage.OpenReadAsync(key, cancellationToken);
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
