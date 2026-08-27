using KindredPaws.Api.Domain.Moderation;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class ReportRepository(AppDbContext db)
{
    public Task AddAsync(Report entity, CancellationToken ct) => db.Reports.AddAsync(entity, ct).AsTask();
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct) => db.Users.AnyAsync(x => x.Id == userId, ct);
    public Task<Report?> GetAsync(Guid id, CancellationToken ct) => db.Reports.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<Report>> ListAsync(ReportStatus? status, ReportTargetType? targetType, CancellationToken ct)
    {
        var query = db.Reports.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (targetType.HasValue) query = query.Where(x => x.TargetType == targetType.Value);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
