using System.Security.Claims;
using KindredPaws.Api.Application.Dashboard;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Roles = Roles.SuperAdministrator)]
    public Task<DashboardSummaryResponse> Summary(CancellationToken ct) => dashboardService.GetGlobalSummaryAsync(ct);

    [HttpGet("my-shelter")]
    [Authorize(Roles = Roles.Administrator)]
    public Task<ShelterDashboardSummaryResponse> MyShelter(CancellationToken ct)
    {
        var shelterId = Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Tu cuenta de Administrador todavía no tiene un refugio asociado.");
        return dashboardService.GetShelterSummaryAsync(shelterId, ct);
    }
}
