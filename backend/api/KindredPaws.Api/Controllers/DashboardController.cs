using KindredPaws.Api.Application.Dashboard;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public Task<DashboardSummaryResponse> Summary(CancellationToken ct) => dashboardService.GetSummaryAsync(ct);
}
