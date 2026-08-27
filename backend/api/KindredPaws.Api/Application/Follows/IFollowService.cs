namespace KindredPaws.Api.Application.Follows;

public interface IFollowService
{
    Task FollowAsync(Guid animalId, Guid userId, CancellationToken cancellationToken);
    Task UnfollowAsync(Guid animalId, Guid userId, CancellationToken cancellationToken);
    Task<FollowSummaryResponse> GetSummaryAsync(Guid animalId, Guid? currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ListFollowerIdsAsync(Guid animalId, CancellationToken cancellationToken);
}
