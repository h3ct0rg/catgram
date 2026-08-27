using KindredPaws.Api.Domain.Audit;

namespace KindredPaws.Api.Application.Audit;

public interface IAuditService
{
    Task RecordAsync(Guid actorUserId, AuditAction action, string entityType, Guid entityId, string? details, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLogResponse>> ListAsync(AuditAction? action, string? entityType, DateTimeOffset? before, int pageSize, CancellationToken cancellationToken);
    Task<int> PurgeOlderThanAsync(int days, CancellationToken cancellationToken);
}
