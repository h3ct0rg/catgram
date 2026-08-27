using KindredPaws.Api.Domain.Follows;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class FollowRepository(AppDbContext db)
{
    public Task<bool> ExistsAsync(Guid animalId, Guid userId, CancellationToken ct) => db.Follows.AnyAsync(x => x.AnimalId == animalId && x.UserId == userId, ct);
    public Task<Follow?> FindAsync(Guid animalId, Guid userId, CancellationToken ct) => db.Follows.SingleOrDefaultAsync(x => x.AnimalId == animalId && x.UserId == userId, ct);
    public Task<int> CountAsync(Guid animalId, CancellationToken ct) => db.Follows.CountAsync(x => x.AnimalId == animalId, ct);
    public async Task<IReadOnlyCollection<Guid>> ListFollowerIdsAsync(Guid animalId, CancellationToken ct) => await db.Follows.AsNoTracking().Where(x => x.AnimalId == animalId).Select(x => x.UserId).ToListAsync(ct);
    public Task AddAsync(Follow entity, CancellationToken ct) => db.Follows.AddAsync(entity, ct).AsTask();
    public void Remove(Follow entity) => db.Follows.Remove(entity);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
