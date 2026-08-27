using System.Security.Claims;
using KindredPaws.Api.Application.Engagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/posts/{postId:guid}/likes")]
public sealed class LikesController(ILikeService likeService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<LikeSummaryResponse> Get(Guid postId, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        return likeService.GetSummaryAsync(postId, userId, ct);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Like(Guid postId, CancellationToken ct)
    {
        await likeService.LikeAsync(postId, Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value), ct);
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Unlike(Guid postId, CancellationToken ct)
    {
        await likeService.UnlikeAsync(postId, Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value), ct);
        return NoContent();
    }
}
