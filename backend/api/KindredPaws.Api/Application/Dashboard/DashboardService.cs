using KindredPaws.Api.Domain.Adoption;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Dashboard;

public sealed class DashboardService(AppDbContext db, UserManager<ApplicationUser> userManager) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetGlobalSummaryAsync(CancellationToken ct)
    {
        var sheltersBreakdown = await db.Shelters.AsNoTracking()
            .Select(s => new ShelterBreakdownItem(s.Id, s.Name, s.Animals.Count, s.Animals.Count(a => a.AdoptionStatus == AdoptionStatus.Adopted)))
            .ToListAsync(ct);

        return new(
            await userManager.Users.CountAsync(ct),
            await db.Shelters.CountAsync(ct),
            await db.Animals.CountAsync(ct),
            await db.Animals.CountAsync(x => x.AdoptionStatus == AdoptionStatus.Adopted, ct),
            await db.Posts.CountAsync(x => x.Visibility == ContentVisibility.Published, ct),
            await db.Stories.CountAsync(x => x.ExpiresAt > DateTimeOffset.UtcNow, ct),
            await db.Likes.CountAsync(ct),
            await db.Comments.CountAsync(x => x.Visibility == ContentVisibility.Published, ct),
            await db.Posts.SumAsync(x => x.ShareCount, ct),
            await db.Posts.SumAsync(x => x.ViewCount, ct),
            sheltersBreakdown,
            await GetTopAnimalsAsync(ct));
    }

    public async Task<ShelterDashboardSummaryResponse> GetShelterSummaryAsync(Guid shelterId, CancellationToken ct)
    {
        var animalIds = await db.Animals.AsNoTracking().Where(a => a.ShelterId == shelterId).Select(a => a.Id).ToListAsync(ct);
        var postIds = await db.Posts.AsNoTracking().Where(p => p.ShelterId == shelterId).Select(p => p.Id).ToListAsync(ct);

        var animals = animalIds.Count;
        var adopted = await db.Animals.CountAsync(a => a.ShelterId == shelterId && a.AdoptionStatus == AdoptionStatus.Adopted, ct);
        var posts = await db.Posts.CountAsync(p => p.ShelterId == shelterId && p.Visibility == ContentVisibility.Published, ct);
        var likes = postIds.Count == 0 ? 0 : await db.Likes.CountAsync(l => postIds.Contains(l.PostId), ct);
        var comments = postIds.Count == 0 ? 0 : await db.Comments.CountAsync(c => postIds.Contains(c.PostId) && c.Visibility == ContentVisibility.Published, ct);
        var shares = await db.Posts.Where(p => p.ShelterId == shelterId).SumAsync(p => p.ShareCount, ct);
        var views = await db.Posts.Where(p => p.ShelterId == shelterId).SumAsync(p => p.ViewCount, ct);
        var pendingRequests = animalIds.Count == 0 ? 0 : await db.AdoptionRequests.CountAsync(r => animalIds.Contains(r.AnimalId) && r.Status == AdoptionRequestStatus.Pending, ct);

        return new(animals, adopted, posts, likes, comments, shares, views, pendingRequests);
    }

    private async Task<IReadOnlyCollection<AnimalEngagementItem>> GetTopAnimalsAsync(CancellationToken ct)
    {
        var posts = await db.Posts.AsNoTracking().Select(p => new { p.Id, p.AnimalId, p.ViewCount, p.ShareCount }).ToListAsync(ct);
        if (posts.Count == 0) return [];

        var likeCounts = await db.Likes.AsNoTracking().GroupBy(l => l.PostId).Select(g => new { PostId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.PostId, x => x.Count, ct);

        var byAnimal = posts.GroupBy(p => p.AnimalId)
            .Select(g => new
            {
                AnimalId = g.Key,
                Likes = g.Sum(p => likeCounts.GetValueOrDefault(p.Id)),
                Shares = g.Sum(p => p.ShareCount),
                Views = g.Sum(p => p.ViewCount),
            })
            .OrderByDescending(x => x.Likes + x.Shares)
            .Take(10)
            .ToList();

        var animalIds = byAnimal.Select(x => x.AnimalId).ToArray();
        var animalsInfo = await db.Animals.AsNoTracking().Include(a => a.Shelter).Where(a => animalIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        return byAnimal.Select(x => new AnimalEngagementItem(
            x.AnimalId,
            animalsInfo.GetValueOrDefault(x.AnimalId)?.Name ?? "",
            animalsInfo.GetValueOrDefault(x.AnimalId)?.Shelter?.Name ?? "",
            x.Likes, x.Shares, x.Views)).ToArray();
    }
}
