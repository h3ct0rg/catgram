using KindredPaws.Api.Domain.Moderation;

namespace KindredPaws.Api.Application.Moderation;

public sealed record CreateReportRequest(ReportTargetType TargetType, Guid TargetId, string Reason);
public sealed record ReportResponse(Guid Id, ReportTargetType TargetType, Guid TargetId, string Reason, ReportStatus Status, DateTimeOffset CreatedAt);
