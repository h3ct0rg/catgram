using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Infrastructure.Persistence;

namespace KindredPaws.Api.Application.Notifications;

public sealed class NotificationService(NotificationRepository repository) : INotificationService
{
    public async Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string body, string? linkUrl, Guid? relatedEntityId, CancellationToken ct)
    {
        if (!await IsEnabledAsync(recipientUserId, type, ct)) return;
        await repository.AddAsync(new Notification { RecipientUserId = recipientUserId, Type = type, Title = title, Body = body, LinkUrl = linkUrl, RelatedEntityId = relatedEntityId }, ct);
        await repository.SaveAsync(ct);
    }

    public async Task<IReadOnlyCollection<NotificationResponse>> ListAsync(Guid userId, DateTimeOffset? before, bool unreadOnly, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = await repository.ListAsync(userId, before, unreadOnly, pageSize, ct);
        return items.Select(ToResponse).ToArray();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct) => repository.CountUnreadAsync(userId, ct);

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var notification = await repository.GetAsync(notificationId, userId, ct) ?? throw new KeyNotFoundException("Notificación no encontrada.");
        if (notification.IsRead) return;
        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        await repository.MarkAllReadAsync(userId, ct);
        await repository.SaveAsync(ct);
    }

    public async Task<IReadOnlyCollection<NotificationPreferenceResponse>> GetPreferencesAsync(Guid userId, CancellationToken ct)
    {
        var overrides = await repository.ListPreferencesAsync(userId, ct);
        return Enum.GetValues<NotificationType>()
            .Select(type => new NotificationPreferenceResponse(type, overrides.SingleOrDefault(x => x.Type == type)?.Enabled ?? true))
            .ToArray();
    }

    public async Task SetPreferenceAsync(Guid userId, NotificationType type, bool enabled, CancellationToken ct)
    {
        var preference = await repository.FindPreferenceAsync(userId, type, ct);
        if (preference is null) await repository.AddPreferenceAsync(new NotificationPreference { UserId = userId, Type = type, Enabled = enabled }, ct);
        else preference.Enabled = enabled;
        await repository.SaveAsync(ct);
    }

    public async Task<bool> IsEnabledAsync(Guid userId, NotificationType type, CancellationToken ct) =>
        (await repository.FindPreferenceAsync(userId, type, ct))?.Enabled ?? true;

    private static NotificationResponse ToResponse(Notification x) => new(x.Id, x.Type, x.Title, x.Body, x.LinkUrl, x.RelatedEntityId, x.IsRead, x.CreatedAt);
}
