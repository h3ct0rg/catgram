using KindredPaws.Api.Application.Animals;
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
    public Task<IReadOnlyCollection<AnimalResponse>> List([FromQuery] Guid? shelterId, CancellationToken cancellationToken) => animalService.ListAsync(shelterId, cancellationToken);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public Task<AnimalResponse> Get(Guid id, CancellationToken cancellationToken) => animalService.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AnimalResponse> Create(CreateAnimalRequest request, CancellationToken cancellationToken) => animalService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public Task<AnimalResponse> Update(Guid id, UpdateAnimalRequest request, CancellationToken cancellationToken) => animalService.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/media")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.SuperAdministrator}")]
    public async Task<AnimalMediaResponse> AddMedia(Guid id, IFormFile file, [FromForm] bool isPrimary, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await animalService.AddMediaAsync(id, new MediaUpload(file.FileName, file.ContentType, file.Length, stream, isPrimary), cancellationToken);
    }
}
