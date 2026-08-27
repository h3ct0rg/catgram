using KindredPaws.Api.Domain.Moderation;

namespace KindredPaws.Api.Application.Moderation;

public interface IReportService
{
    Task<ReportResponse> CreateAsync(Guid reporterId, CreateReportRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ReportResponse>> ListAsync(ReportStatus? status, ReportTargetType? targetType, CancellationToken cancellationToken);
    Task<ReportResponse> ResolveAsync(Guid reportId, ReportStatus status, Guid actorUserId, CancellationToken cancellationToken);
}
