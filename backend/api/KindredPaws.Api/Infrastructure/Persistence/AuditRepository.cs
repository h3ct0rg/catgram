using KindredPaws.Api.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class AuditRepository(AppDbContext db)
{
    public Task AddAsync(AuditLog entity, CancellationToken ct) => db.AuditLogs.AddAsync(entity, ct).AsTask();

    public async Task<IReadOnlyCollection<AuditLog>> ListAsync(AuditAction? action, string? entityType, DateTimeOffset? before, int pageSize, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (action.HasValue) query = query.Where(x => x.Action == action.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (before.HasValue) query = query.Where(x => x.CreatedAt < before.Value);
        return await query.OrderByDescending(x => x.CreatedAt).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        var old = await db.AuditLogs.Where(x => x.CreatedAt < cutoff).ToListAsync(ct);
        db.AuditLogs.RemoveRange(old);
        await db.SaveChangesAsync(ct);
        return old.Count;
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
