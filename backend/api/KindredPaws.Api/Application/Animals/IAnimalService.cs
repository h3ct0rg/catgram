namespace KindredPaws.Api.Application.Animals;

public interface IAnimalService
{
    Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ShelterResponse>> ListSheltersAsync(CancellationToken cancellationToken);
    Task<AnimalResponse> CreateAsync(CreateAnimalRequest request, CancellationToken cancellationToken);
    Task<AnimalResponse> UpdateAsync(Guid id, UpdateAnimalRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<AnimalResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AnimalResponse>> ListAsync(Guid? shelterId, CancellationToken cancellationToken);
    Task<AnimalMediaResponse> AddMediaAsync(Guid animalId, MediaUpload upload, CancellationToken cancellationToken);
    Task<AnimalStatsResponse> GetStatsAsync(Guid animalId, CancellationToken cancellationToken);
}
