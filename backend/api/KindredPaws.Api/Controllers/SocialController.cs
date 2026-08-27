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
    public Task<IReadOnlyCollection<PostResponse>> Feed([FromQuery] DateTimeOffset? before, [FromQuery] int pageSize = 20, CancellationToken ct = default) => socialService.GetFeedAsync(before, pageSize, ct);

    [HttpGet("stories")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<StoryResponse>> Stories(CancellationToken ct) => socialService.GetStoriesAsync(ct);

    [HttpPost("posts")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<PostResponse> CreatePost([FromForm] CreatePostRequest request, [FromForm] List<IFormFile>? files, CancellationToken ct)
    {
        var uploads = (files ?? []).Select(file => new MediaUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream(), false)).ToArray();
        try { return await socialService.CreatePostAsync(request, uploads, ct); }
        finally { foreach (var upload in uploads) await upload.Content.DisposeAsync(); }
    }

    [HttpPut("posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<PostResponse> UpdatePost(Guid id, UpdatePostRequest request, CancellationToken ct) => socialService.UpdatePostAsync(id, request, ct);

    [HttpDelete("posts/{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<IActionResult> HidePost(Guid id, CancellationToken ct) { await socialService.HidePostAsync(id, ct); return NoContent(); }

    [HttpPost("stories")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<StoryResponse> CreateStory([FromForm] CreateStoryRequest request, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream(); return await socialService.CreateStoryAsync(request, new MediaUpload(file.FileName, file.ContentType, file.Length, stream, true), ct);
    }

    [HttpPost("stories/{id:guid}/views")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewStory(Guid id, [FromHeader(Name = "X-Anonymous-Key")] string? anonymousKey, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        await socialService.RegisterStoryViewAsync(id, anonymousKey, userId, ct); return NoContent();
    }
}
