using System.Security.Claims;
using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Application.Social;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/social")]
public sealed class SocialController(ISocialService socialService) : ControllerBase
{
    [HttpGet("feed")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<PostResponse>> Feed([FromQuery] DateTimeOffset? before, [FromQuery] int skip = 0, [FromQuery] int pageSize = 20, [FromQuery] string sort = "recent", [FromQuery] bool successStoriesOnly = false, CancellationToken ct = default)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        return socialService.GetFeedAsync(before, skip, pageSize, sort, successStoriesOnly, userId, ct);
    }

    [HttpGet("stories")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<StoryResponse>> Stories(CancellationToken ct) => socialService.GetStoriesAsync(ct);

    [HttpGet("posts/{id:guid}")]
    [AllowAnonymous]
    public async Task<PostResponse> GetPost(Guid id, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        await socialService.RegisterPostViewAsync(id, ct);
        return await socialService.GetPostAsync(id, userId, ct);
    }

    [HttpPost("posts/{id:guid}/shares")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterShare(Guid id, CancellationToken ct)
    {
        await socialService.RegisterPostShareAsync(id, ct);
        return NoContent();
    }

    [HttpPost("posts")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<PostResponse> CreatePost([FromForm] CreatePostRequest request, [FromForm] List<IFormFile>? files, CancellationToken ct)
    {
        var uploads = (files ?? []).Select(file => new MediaUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream(), false)).ToArray();
        try { return await socialService.CreatePostAsync(request, ActorUserId, ActorShelterId, uploads, ct); }
        finally { foreach (var upload in uploads) await upload.Content.DisposeAsync(); }
    }

    [HttpPut("posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<PostResponse> UpdatePost(Guid id, UpdatePostRequest request, CancellationToken ct) => socialService.UpdatePostAsync(id, request, ActorShelterId, ct);

    [HttpDelete("posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<IActionResult> HidePost(Guid id, CancellationToken ct)
    {
        await socialService.HidePostAsync(id, ActorUserId, ActorShelterId, ct);
        return NoContent();
    }

    [HttpPost("stories")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<StoryResponse> CreateStory([FromForm] CreateStoryRequest request, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await socialService.CreateStoryAsync(request, ActorShelterId, new MediaUpload(file.FileName, file.ContentType, file.Length, stream, true), ct);
    }

    [HttpPost("stories/{id:guid}/views")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewStory(Guid id, [FromHeader(Name = "X-Anonymous-Key")] string? anonymousKey, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        await socialService.RegisterStoryViewAsync(id, anonymousKey, userId, ct); return NoContent();
    }

    private Guid ActorUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Null for SuperAdministrador (unrestricted); the caller's own shelter for a scoped Administrador.</summary>
    private Guid? ActorShelterId => Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var shelterId) ? shelterId : null;
}
