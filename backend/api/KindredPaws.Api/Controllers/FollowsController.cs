using System.Security.Claims;
using KindredPaws.Api.Application.Follows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/animals/{animalId:guid}/follow")]
public sealed class FollowsController(IFollowService followService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<FollowSummaryResponse> Get(Guid animalId, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        return followService.GetSummaryAsync(animalId, userId, ct);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Follow(Guid animalId, CancellationToken ct)
    {
        await followService.FollowAsync(animalId, Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value), ct);
        return NoContent();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Unfollow(Guid animalId, CancellationToken ct)
    {
        await followService.UnfollowAsync(animalId, Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value), ct);
        return NoContent();
    }
}
