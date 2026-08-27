using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Audit;

public sealed class AuditService(AuditRepository repository, UserManager<ApplicationUser> userManager) : IAuditService
{
    public async Task RecordAsync(Guid actorUserId, AuditAction action, string entityType, Guid entityId, string? details, CancellationToken ct)
    {
        await repository.AddAsync(new AuditLog { ActorUserId = actorUserId, Action = action, EntityType = entityType, EntityId = entityId, Details = details }, ct);
        await repository.SaveAsync(ct);
    }

    public async Task<IReadOnlyCollection<AuditLogResponse>> ListAsync(AuditAction? action, string? entityType, DateTimeOffset? before, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var logs = await repository.ListAsync(action, entityType, before, pageSize, ct);
        if (logs.Count == 0) return [];
        var actorIds = logs.Select(x => x.ActorUserId).Distinct().ToArray();
        var actors = await userManager.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        return logs.Select(x => new AuditLogResponse(x.Id, x.ActorUserId, actors.GetValueOrDefault(x.ActorUserId)?.UserName ?? "desconocido", x.Action, x.EntityType, x.EntityId, x.Details, x.CreatedAt)).ToArray();
    }

    public Task<int> PurgeOlderThanAsync(int days, CancellationToken ct) => repository.PurgeOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-days), ct);
}
