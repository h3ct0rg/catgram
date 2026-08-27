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
    public Task<IReadOnlyCollection<ShelterResponse>> List(CancellationToken cancellationToken) => animalService.ListSheltersAsync(cancellationToken);

    [HttpPost]
    [Authorize(Roles = Roles.SuperAdministrator)]
    public Task<ShelterResponse> Create(CreateShelterRequest request, CancellationToken cancellationToken) => animalService.CreateShelterAsync(request, cancellationToken);
}
