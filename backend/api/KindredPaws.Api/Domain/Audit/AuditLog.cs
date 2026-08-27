namespace KindredPaws.Api.Domain.Audit;

public enum AuditAction { UserActivated, UserDeactivated, UserRoleChanged, PostHidden, CommentHidden, AdoptionStatusChanged, ReportResolved }

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
