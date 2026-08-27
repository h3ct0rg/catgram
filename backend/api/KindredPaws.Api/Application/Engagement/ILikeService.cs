namespace KindredPaws.Api.Application.Engagement;

public interface ILikeService
{
    Task LikeAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task UnlikeAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task<LikeSummaryResponse> GetSummaryAsync(Guid postId, Guid? currentUserId, CancellationToken cancellationToken);
}
