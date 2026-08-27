using System.Security.Claims;
using KindredPaws.Api.Application.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpPost]
    public Task<ReportResponse> Create(CreateReportRequest request, CancellationToken ct)
    {
        var reporterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return reportService.CreateAsync(reporterId, request, ct);
    }
}
