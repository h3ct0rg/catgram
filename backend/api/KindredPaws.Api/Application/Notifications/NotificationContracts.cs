using KindredPaws.Api.Domain.Notifications;

namespace KindredPaws.Api.Application.Notifications;

public sealed record NotificationResponse(Guid Id, NotificationType Type, string Title, string Body, string? LinkUrl, Guid? RelatedEntityId, bool IsRead, DateTimeOffset CreatedAt);
public sealed record NotificationPreferenceResponse(NotificationType Type, bool Enabled);
public sealed record UpdateNotificationPreferenceRequest(bool Enabled);
