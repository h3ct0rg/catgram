using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class SocialRepository(AppDbContext db)
{
    public Task<Animal?> GetAnimalAsync(Guid animalId, Guid shelterId, CancellationToken ct) => db.Animals.Include(x => x.Shelter).SingleOrDefaultAsync(x => x.Id == animalId && x.ShelterId == shelterId, ct);
    public Task AddPostAsync(Post post, CancellationToken ct) { db.Posts.Add(post); return Task.CompletedTask; }
    public Task<Post?> GetPostAsync(Guid id, CancellationToken ct) => db.Posts.Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<Post>> ListFeedAsync(DateTimeOffset? before, int skip, int pageSize, bool popular, bool successStoriesOnly, CancellationToken ct)
    {
        var query = db.Posts.AsNoTracking().Include(x => x.Media).Where(x => x.Visibility == ContentVisibility.Published);
        if (successStoriesOnly) query = query.Where(x => x.IsSuccessStory);
        if (popular)
        {
            return await query
                .OrderByDescending(p => db.Likes.Count(l => l.PostId == p.Id))
                .ThenByDescending(p => p.CreatedAt)
                .Skip(skip).Take(pageSize).ToListAsync(ct);
        }
        if (before.HasValue) query = query.Where(x => x.CreatedAt < before.Value);
        return await query.OrderByDescending(x => x.CreatedAt).Take(pageSize).ToListAsync(ct);
    }
    public Task AddStoryAsync(Story story, CancellationToken ct) { db.Stories.Add(story); return Task.CompletedTask; }
    public Task<bool> AnimalExistsAsync(Guid animalId, Guid shelterId, CancellationToken ct) => db.Animals.AnyAsync(x => x.Id == animalId && x.ShelterId == shelterId, ct);
    public async Task<IReadOnlyDictionary<Guid, Animal>> GetAnimalsByIdsAsync(IReadOnlyCollection<Guid> animalIds, CancellationToken ct) =>
        await db.Animals.AsNoTracking().Include(x => x.Shelter).Where(x => animalIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
    public async Task<IReadOnlyCollection<Story>> ListStoriesAsync(CancellationToken ct) => await db.Stories.AsNoTracking().Include(x => x.Views).Where(x => x.ExpiresAt > DateTimeOffset.UtcNow).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public Task<Story?> GetStoryAsync(Guid id, CancellationToken ct) => db.Stories.SingleOrDefaultAsync(x => x.Id == id && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    public Task AddViewAsync(StoryView view, CancellationToken ct) { db.StoryViews.Add(view); return Task.CompletedTask; }

    public Task IncrementViewCountAsync(Guid postId, CancellationToken ct) =>
        db.Posts.Where(x => x.Id == postId).ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

    public Task IncrementShareCountAsync(Guid postId, CancellationToken ct) =>
        db.Posts.Where(x => x.Id == postId).ExecuteUpdateAsync(s => s.SetProperty(p => p.ShareCount, p => p.ShareCount + 1), ct);

    public async Task<(int PostCount, int TotalViews, int TotalShares, IReadOnlyCollection<Guid> PostIds)> GetAnimalPostStatsAsync(Guid animalId, CancellationToken ct)
    {
        var posts = await db.Posts.AsNoTracking().Where(x => x.AnimalId == animalId && x.Visibility == ContentVisibility.Published)
            .Select(x => new { x.Id, x.ViewCount, x.ShareCount }).ToListAsync(ct);
        return (posts.Count, posts.Sum(x => x.ViewCount), posts.Sum(x => x.ShareCount), posts.Select(x => x.Id).ToArray());
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
