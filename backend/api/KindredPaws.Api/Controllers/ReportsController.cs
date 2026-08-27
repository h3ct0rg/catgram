using System.Security.Claims;
using KindredPaws.Api.Application.Moderation;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpPost]
    public Task<ReportResponse> Create(CreateReportRequest request, CancellationToken ct) => reportService.CreateAsync(CurrentUserId, request, ct);

    [HttpGet]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<IReadOnlyCollection<ReportResponse>> List([FromQuery] ReportStatus? status, [FromQuery] ReportTargetType? targetType, CancellationToken ct) =>
        reportService.ListAsync(status, targetType, ct);

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<ReportResponse> Resolve(Guid id, ResolveReportRequest request, CancellationToken ct) => reportService.ResolveAsync(id, request.Status, CurrentUserId, ct);

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
