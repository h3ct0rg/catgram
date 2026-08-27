using System.Security.Claims;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<NotificationResponse>> List([FromQuery] DateTimeOffset? before, [FromQuery] bool unreadOnly = false, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        notificationService.ListAsync(CurrentUserId, before, unreadOnly, pageSize, ct);

    [HttpGet("unread-count")]
    public Task<int> UnreadCount(CancellationToken ct) => notificationService.GetUnreadCountAsync(CurrentUserId, ct);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await notificationService.MarkReadAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(CurrentUserId, ct);
        return NoContent();
    }

    [HttpGet("preferences")]
    public Task<IReadOnlyCollection<NotificationPreferenceResponse>> GetPreferences(CancellationToken ct) => notificationService.GetPreferencesAsync(CurrentUserId, ct);

    [HttpPut("preferences/{type}")]
    public async Task<IActionResult> SetPreference(NotificationType type, UpdateNotificationPreferenceRequest request, CancellationToken ct)
    {
        await notificationService.SetPreferenceAsync(CurrentUserId, type, request.Enabled, ct);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
