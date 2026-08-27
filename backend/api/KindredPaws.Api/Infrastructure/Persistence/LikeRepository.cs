using KindredPaws.Api.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class LikeRepository(AppDbContext db)
{
    public Task<bool> ExistsAsync(Guid postId, Guid userId, CancellationToken ct) => db.Likes.AnyAsync(x => x.PostId == postId && x.UserId == userId, ct);
    public Task<Like?> FindAsync(Guid postId, Guid userId, CancellationToken ct) => db.Likes.SingleOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, ct);
    public Task<int> CountAsync(Guid postId, CancellationToken ct) => db.Likes.CountAsync(x => x.PostId == postId, ct);
    public async Task<IReadOnlyDictionary<Guid, int>> CountManyAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct) =>
        await db.Likes.AsNoTracking().Where(x => postIds.Contains(x.PostId)).GroupBy(x => x.PostId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    public async Task<HashSet<Guid>> ListLikedPostIdsAsync(Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct) =>
        (await db.Likes.AsNoTracking().Where(x => x.UserId == userId && postIds.Contains(x.PostId)).Select(x => x.PostId).ToListAsync(ct)).ToHashSet();
    public Task AddAsync(Like entity, CancellationToken ct) => db.Likes.AddAsync(entity, ct).AsTask();
    public void Remove(Like entity) => db.Likes.Remove(entity);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
