using KindredPaws.Api.Domain.Notifications;

namespace KindredPaws.Api.Application.Notifications;

public interface INotificationService
{
    Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string body, string? linkUrl, Guid? relatedEntityId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NotificationResponse>> ListAsync(Guid userId, DateTimeOffset? before, bool unreadOnly, int pageSize, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NotificationPreferenceResponse>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken);
    Task SetPreferenceAsync(Guid userId, NotificationType type, bool enabled, CancellationToken cancellationToken);
    Task<bool> IsEnabledAsync(Guid userId, NotificationType type, CancellationToken cancellationToken);
}
