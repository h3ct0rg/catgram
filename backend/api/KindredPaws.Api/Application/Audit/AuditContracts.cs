using KindredPaws.Api.Domain.Audit;

namespace KindredPaws.Api.Application.Audit;

public sealed record AuditLogResponse(Guid Id, Guid ActorUserId, string ActorUserName, AuditAction Action, string EntityType, Guid EntityId, string? Details, DateTimeOffset CreatedAt);
