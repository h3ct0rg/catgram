using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = Roles.SuperAdministrator)]
public sealed class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<AuditLogResponse>> List([FromQuery] AuditAction? action, [FromQuery] string? entityType, [FromQuery] DateTimeOffset? before, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        auditService.ListAsync(action, entityType, before, pageSize, ct);

    [HttpDelete("purge")]
    public async Task<IActionResult> Purge([FromQuery] int olderThanDays = 180, CancellationToken ct = default)
    {
        var count = await auditService.PurgeOlderThanAsync(olderThanDays, ct);
        return Ok(new { purged = count });
    }
}
