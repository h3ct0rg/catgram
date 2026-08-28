using System.Security.Claims;
using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/animals")]
public sealed class AnimalsController(IAnimalService animalService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<AnimalResponse>> List(
        [FromQuery] Guid? shelterId,
        [FromQuery] string? name,
        [FromQuery] AnimalSpecies? species,
        [FromQuery] AnimalSex? sex,
        [FromQuery] AnimalSize? size,
        [FromQuery] string? breed,
        [FromQuery] string? location,
        [FromQuery] AdoptionStatus? adoptionStatus,
        CancellationToken cancellationToken) =>
        animalService.ListAsync(new AnimalSearchFilter(shelterId, name, species, sex, size, breed, location, adoptionStatus), cancellationToken);

    [HttpGet("nearby")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<AnimalResponse>> Nearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm = 25, CancellationToken cancellationToken = default) =>
        animalService.ListNearbyAsync(lat, lng, radiusKm, cancellationToken);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public Task<AnimalResponse> Get(Guid id, CancellationToken cancellationToken) => animalService.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AnimalResponse> Create(CreateAnimalRequest request, CancellationToken cancellationToken) => animalService.CreateAsync(request, ActorShelterId, cancellationToken);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AnimalResponse> Update(Guid id, UpdateAnimalRequest request, CancellationToken cancellationToken) =>
        animalService.UpdateAsync(id, request, ActorUserId, ActorShelterId, cancellationToken);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await animalService.DeleteAsync(id, ActorShelterId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AnimalStatsResponse> GetStats(Guid id, CancellationToken cancellationToken) => animalService.GetStatsAsync(id, ActorShelterId, cancellationToken);

    [HttpPost("{id:guid}/media")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<AnimalMediaResponse> AddMedia(Guid id, IFormFile file, [FromForm] bool isPrimary, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await animalService.AddMediaAsync(id, new MediaUpload(file.FileName, file.ContentType, file.Length, stream, isPrimary), ActorShelterId, cancellationToken);
    }

    private Guid ActorUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Null for SuperAdministrador (unrestricted); the caller's own shelter for a scoped Administrador.</summary>
    private Guid? ActorShelterId => Guid.TryParse(User.FindFirst("shelter_id")?.Value, out var shelterId) ? shelterId : null;
}
