using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class SocialRepository(AppDbContext db)
{
    public Task<Animal?> GetAnimalAsync(Guid animalId, Guid shelterId, CancellationToken ct) => db.Animals.Include(x => x.Shelter).SingleOrDefaultAsync(x => x.Id == animalId && x.ShelterId == shelterId, ct);
    public Task AddPostAsync(Post post, CancellationToken ct) { db.Posts.Add(post); return Task.CompletedTask; }
    public Task<Post?> GetPostAsync(Guid id, CancellationToken ct) => db.Posts.Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<Post>> ListFeedAsync(DateTimeOffset? before, int pageSize, CancellationToken ct)
    {
        var query = db.Posts.AsNoTracking().Include(x => x.Media).Where(x => x.Visibility == ContentVisibility.Published);
        if (before.HasValue) query = query.Where(x => x.CreatedAt < before.Value);
        return await query.OrderByDescending(x => x.CreatedAt).Take(pageSize).ToListAsync(ct);
    }
    public Task AddStoryAsync(Story story, CancellationToken ct) { db.Stories.Add(story); return Task.CompletedTask; }
    public Task<bool> AnimalExistsAsync(Guid animalId, Guid shelterId, CancellationToken ct) => db.Animals.AnyAsync(x => x.Id == animalId && x.ShelterId == shelterId, ct);
    public async Task<IReadOnlyCollection<Story>> ListStoriesAsync(CancellationToken ct) => await db.Stories.AsNoTracking().Include(x => x.Views).Where(x => x.ExpiresAt > DateTimeOffset.UtcNow).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public Task<Story?> GetStoryAsync(Guid id, CancellationToken ct) => db.Stories.SingleOrDefaultAsync(x => x.Id == id && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    public Task AddViewAsync(StoryView view, CancellationToken ct) { db.StoryViews.Add(view); return Task.CompletedTask; }
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
