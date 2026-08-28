using KindredPaws.Api.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class CommentLikeRepository(AppDbContext db)
{
    public Task<bool> ExistsAsync(Guid commentId, Guid userId, CancellationToken ct) =>
        db.CommentLikes.AnyAsync(x => x.CommentId == commentId && x.UserId == userId, ct);
    public Task<CommentLike?> FindAsync(Guid commentId, Guid userId, CancellationToken ct) =>
        db.CommentLikes.SingleOrDefaultAsync(x => x.CommentId == commentId && x.UserId == userId, ct);
    public async Task<IReadOnlyDictionary<Guid, int>> CountManyAsync(IReadOnlyCollection<Guid> commentIds, CancellationToken ct) =>
        await db.CommentLikes.AsNoTracking().Where(x => commentIds.Contains(x.CommentId)).GroupBy(x => x.CommentId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    public async Task<HashSet<Guid>> ListLikedCommentIdsAsync(Guid userId, IReadOnlyCollection<Guid> commentIds, CancellationToken ct) =>
        (await db.CommentLikes.AsNoTracking().Where(x => x.UserId == userId && commentIds.Contains(x.CommentId)).Select(x => x.CommentId).ToListAsync(ct)).ToHashSet();
    public Task AddAsync(CommentLike entity, CancellationToken ct) => db.CommentLikes.AddAsync(entity, ct).AsTask();
    public void Remove(CommentLike entity) => db.CommentLikes.Remove(entity);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
