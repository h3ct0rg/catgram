namespace KindredPaws.Api.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetGlobalSummaryAsync(CancellationToken cancellationToken);
    Task<ShelterDashboardSummaryResponse> GetShelterSummaryAsync(Guid shelterId, CancellationToken cancellationToken);
}
