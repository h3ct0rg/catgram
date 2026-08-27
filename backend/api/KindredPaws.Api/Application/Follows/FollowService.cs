using KindredPaws.Api.Domain.Follows;
using KindredPaws.Api.Infrastructure.Persistence;

namespace KindredPaws.Api.Application.Follows;

public sealed class FollowService(FollowRepository follows, AnimalRepository animals) : IFollowService
{
    public async Task FollowAsync(Guid animalId, Guid userId, CancellationToken ct)
    {
        _ = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        if (await follows.ExistsAsync(animalId, userId, ct)) return;
        await follows.AddAsync(new Follow { AnimalId = animalId, UserId = userId }, ct);
        await follows.SaveAsync(ct);
    }

    public async Task UnfollowAsync(Guid animalId, Guid userId, CancellationToken ct)
    {
        var follow = await follows.FindAsync(animalId, userId, ct);
        if (follow is null) return;
        follows.Remove(follow);
        await follows.SaveAsync(ct);
    }

    public async Task<FollowSummaryResponse> GetSummaryAsync(Guid animalId, Guid? currentUserId, CancellationToken ct)
    {
        var count = await follows.CountAsync(animalId, ct);
        var following = currentUserId.HasValue && await follows.ExistsAsync(animalId, currentUserId.Value, ct);
        return new FollowSummaryResponse(count, following);
    }

    public Task<IReadOnlyCollection<Guid>> ListFollowerIdsAsync(Guid animalId, CancellationToken ct) => follows.ListFollowerIdsAsync(animalId, ct);
}
