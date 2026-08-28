using System.Security.Claims;
using KindredPaws.Api.Application.Adoption;
using KindredPaws.Api.Domain.Adoption;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
public sealed class AdoptionRequestsController(IAdoptionService adoptionService) : ControllerBase
{
    [HttpPost("api/v1/animals/{animalId:guid}/adoption-requests")]
    [Authorize]
    public Task<AdoptionRequestResponse> Create(Guid animalId, CreateAdoptionRequestRequest request, CancellationToken ct) =>
        adoptionService.CreateAsync(animalId, ActorUserId, request, ct);

    [HttpGet("api/v1/adoption-requests")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<IReadOnlyCollection<AdoptionRequestResponse>> List([FromQuery] AdoptionRequestStatus? status, [FromQuery] Guid? animalId, CancellationToken ct) =>
        adoptionService.ListAsync(status, animalId, ActorShelterId, ct);

    [HttpGet("api/v1/adoption-requests/mine")]
    [Authorize]
    public Task<IReadOnlyCollection<AdoptionRequestResponse>> Mine(CancellationToken ct) => adoptionService.ListMineAsync(ActorUserId, ct);

    [HttpPost("api/v1/adoption-requests/{id:guid}/status")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AdoptionRequestResponse> UpdateStatus(Guid id, UpdateAdoptionRequestStatusRequest request, CancellationToken ct) =>
        adoptionService.UpdateStatusAsync(id, request.Status, request.ReviewNotes, ActorUserId, ActorShelterId, ct);

    private Guid ActorUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Null for SuperAdministrador (unrestricted); the caller's own shelter for a scoped Administrador.</summary>
    private Guid? ActorShelterId => Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var shelterId) ? shelterId : null;
}
