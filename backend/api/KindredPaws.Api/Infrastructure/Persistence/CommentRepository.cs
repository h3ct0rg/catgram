using KindredPaws.Api.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class CommentRepository(AppDbContext db)
{
    public Task AddAsync(Comment entity, CancellationToken ct) => db.Comments.AddAsync(entity, ct).AsTask();
    public Task<Comment?> GetAsync(Guid id, CancellationToken ct) => db.Comments.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<int> CountAsync(Guid postId, CancellationToken ct) => db.Comments.CountAsync(x => x.PostId == postId && x.Visibility == ContentVisibility.Published, ct);
    public async Task<IReadOnlyCollection<Comment>> ListByPostAsync(Guid postId, CancellationToken ct) =>
        await db.Comments.AsNoTracking().Where(x => x.PostId == postId && x.Visibility == ContentVisibility.Published).OrderBy(x => x.CreatedAt).ToListAsync(ct);
    public async Task<IReadOnlyDictionary<Guid, int>> CountManyAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct) =>
        await db.Comments.AsNoTracking().Where(x => postIds.Contains(x.PostId) && x.Visibility == ContentVisibility.Published).GroupBy(x => x.PostId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
