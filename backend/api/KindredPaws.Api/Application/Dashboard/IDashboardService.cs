namespace KindredPaws.Api.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
