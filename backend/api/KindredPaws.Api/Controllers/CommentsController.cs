using System.Security.Claims;
using KindredPaws.Api.Application.Social;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
public sealed class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpGet("api/v1/posts/{postId:guid}/comments")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<CommentResponse>> List(Guid postId, CancellationToken ct)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        return commentService.ListAsync(postId, userId, ct);
    }

    [HttpPost("api/v1/posts/{postId:guid}/comments")]
    [Authorize]
    public Task<CommentResponse> Create(Guid postId, CreateCommentRequest request, CancellationToken ct) => commentService.CreateAsync(postId, CurrentUserId, request, ct);

    [HttpDelete("api/v1/comments/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await commentService.DeleteOwnAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    [HttpPost("api/v1/comments/{id:guid}/hide")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
    {
        await commentService.HideAsync(id, ct);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
