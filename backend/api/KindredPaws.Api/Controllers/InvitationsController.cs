using KindredPaws.Api.Application.Auth;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/invitations")]
[Authorize(Roles = Roles.SuperAdministrator)]
public sealed class InvitationsController(IInvitationService invitationService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<InvitationResponse>> List(CancellationToken cancellationToken) => invitationService.ListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<InvitationResponse>> Create(CreateInvitationRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await invitationService.CreateAsync(request.Email, request.FullName, request.Role, request.ShelterId, request.NewShelterName, userId, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await invitationService.RevokeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<ActionResult<InvitationResponse>> Resend(Guid id, CancellationToken cancellationToken) =>
        Ok(await invitationService.ResendAsync(id, cancellationToken));
}

public sealed record CreateInvitationRequest(string Email, string FullName, string Role, Guid? ShelterId, string? NewShelterName);
