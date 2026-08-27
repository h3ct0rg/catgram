using KindredPaws.Api.Domain.Moderation;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class ReportRepository(AppDbContext db)
{
    public Task AddAsync(Report entity, CancellationToken ct) => db.Reports.AddAsync(entity, ct).AsTask();
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct) => db.Users.AnyAsync(x => x.Id == userId, ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
