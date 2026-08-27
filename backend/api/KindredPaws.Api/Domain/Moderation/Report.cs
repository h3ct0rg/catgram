namespace KindredPaws.Api.Domain.Moderation;

public enum ReportTargetType { Post, Comment, User }
public enum ReportStatus { Pending, Reviewed, Resolved, Dismissed }

public sealed class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
