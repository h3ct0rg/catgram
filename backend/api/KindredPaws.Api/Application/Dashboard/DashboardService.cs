using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Dashboard;

public sealed class DashboardService(AppDbContext db, UserManager<ApplicationUser> userManager) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken ct) => new(
        await userManager.Users.CountAsync(ct),
        await db.Shelters.CountAsync(ct),
        await db.Animals.CountAsync(ct),
        await db.Animals.CountAsync(x => x.AdoptionStatus == AdoptionStatus.Adopted, ct),
        await db.Posts.CountAsync(x => x.Visibility == ContentVisibility.Published, ct),
        await db.Stories.CountAsync(x => x.ExpiresAt > DateTimeOffset.UtcNow, ct),
        await db.Likes.CountAsync(ct),
        await db.Comments.CountAsync(x => x.Visibility == ContentVisibility.Published, ct),
        await db.Posts.SumAsync(x => x.ShareCount, ct),
        await db.Posts.SumAsync(x => x.ViewCount, ct));
}
