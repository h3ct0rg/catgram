using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/shelters")]
public sealed class SheltersController(IAnimalService animalService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<ShelterResponse>> List([FromQuery] string? name, CancellationToken cancellationToken) => animalService.ListSheltersAsync(name, cancellationToken);

    [HttpPost]
    [Authorize(Roles = Roles.SuperAdministrator)]
    public Task<ShelterResponse> Create(CreateShelterRequest request, CancellationToken cancellationToken) => animalService.CreateShelterAsync(request, cancellationToken);

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Administrator)]
    public Task<ShelterResponse> GetMine(CancellationToken cancellationToken) =>
        animalService.GetShelterAsync(
            Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var parsed) ? parsed : throw new InvalidOperationException("Tu cuenta de Administrador todavía no tiene un refugio asociado."),
            cancellationToken);

    [HttpPut("mine")]
    [Authorize(Roles = Roles.Administrator)]
    public Task<ShelterResponse> UpdateMine(UpdateShelterRequest request, CancellationToken cancellationToken)
    {
        var shelterId = Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Tu cuenta de Administrador todavía no tiene un refugio asociado.");
        return animalService.UpdateShelterAsync(shelterId, request, cancellationToken);
    }
}
