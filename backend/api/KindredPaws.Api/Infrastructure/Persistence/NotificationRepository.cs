using KindredPaws.Api.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class NotificationRepository(AppDbContext db)
{
    public Task AddAsync(Notification entity, CancellationToken ct) => db.Notifications.AddAsync(entity, ct).AsTask();
    public Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken ct) => db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, ct);

    public async Task<IReadOnlyCollection<Notification>> ListAsync(Guid userId, DateTimeOffset? before, bool unreadOnly, int pageSize, CancellationToken ct)
    {
        var query = db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == userId);
        if (unreadOnly) query = query.Where(x => !x.IsRead);
        if (before.HasValue) query = query.Where(x => x.CreatedAt < before.Value);
        return await query.OrderByDescending(x => x.CreatedAt).Take(pageSize).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct) => db.Notifications.CountAsync(x => x.RecipientUserId == userId && !x.IsRead, ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        var unread = await db.Notifications.Where(x => x.RecipientUserId == userId && !x.IsRead).ToListAsync(ct);
        foreach (var notification in unread) { notification.IsRead = true; notification.ReadAt = DateTimeOffset.UtcNow; }
    }

    public Task<NotificationPreference?> FindPreferenceAsync(Guid userId, NotificationType type, CancellationToken ct) => db.NotificationPreferences.SingleOrDefaultAsync(x => x.UserId == userId && x.Type == type, ct);
    public async Task<IReadOnlyCollection<NotificationPreference>> ListPreferencesAsync(Guid userId, CancellationToken ct) => await db.NotificationPreferences.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct);
    public Task AddPreferenceAsync(NotificationPreference entity, CancellationToken ct) => db.NotificationPreferences.AddAsync(entity, ct).AsTask();
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
