namespace KindredPaws.Api.Application.Moderation;

public interface IReportService
{
    Task<ReportResponse> CreateAsync(Guid reporterId, CreateReportRequest request, CancellationToken cancellationToken);
}
