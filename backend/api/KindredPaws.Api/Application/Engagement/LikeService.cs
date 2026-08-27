using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Contracts;
using Microsoft.AspNetCore.Identity;

namespace KindredPaws.Api.Application.Engagement;

public sealed class LikeService(LikeRepository likes, SocialRepository posts, UserManager<ApplicationUser> userManager, INotificationService notifications, IEventPublisher eventPublisher) : ILikeService
{
    public async Task LikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var post = await posts.GetPostAsync(postId, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        if (await likes.ExistsAsync(postId, userId, ct)) return;
        await likes.AddAsync(new Like { PostId = postId, UserId = userId }, ct);
        await likes.SaveAsync(ct);

        if (post.CreatedByUserId is { } ownerId && ownerId != userId)
        {
            await notifications.CreateAsync(ownerId, NotificationType.Like, "Nuevo like", "A alguien le gustó tu publicación.", $"/p/{postId}", postId, ct);
            var owner = await userManager.FindByIdAsync(ownerId.ToString());
            if (owner is not null)
                await eventPublisher.PublishAsync(new LikeCreatedEvent(postId, ownerId, owner.Email ?? string.Empty, owner.FullName, userId, DateTimeOffset.UtcNow), ct);
        }
    }

    public async Task UnlikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var like = await likes.FindAsync(postId, userId, ct);
        if (like is null) return;
        likes.Remove(like);
        await likes.SaveAsync(ct);
    }

    public async Task<LikeSummaryResponse> GetSummaryAsync(Guid postId, Guid? currentUserId, CancellationToken ct)
    {
        var count = await likes.CountAsync(postId, ct);
        var liked = currentUserId.HasValue && await likes.ExistsAsync(postId, currentUserId.Value, ct);
        return new LikeSummaryResponse(count, liked);
    }
}
