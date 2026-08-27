using KindredPaws.Api.Application.Auth;
using KindredPaws.Api.Application.Users;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = Roles.SuperAdministrator)]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<UserSummary>> List(CancellationToken cancellationToken) => userService.ListAsync(cancellationToken);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetStatusRequest request, CancellationToken cancellationToken)
    {
        await userService.SetActiveAsync(id, request.Active, CurrentUserId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await userService.AssignRoleAsync(id, request.Role, CurrentUserId, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

public sealed record SetStatusRequest(bool Active);
public sealed record AssignRoleRequest(string Role);
