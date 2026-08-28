namespace KindredPaws.Api.Application.Animals;

public interface IAnimalService
{
    Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ShelterResponse>> ListSheltersAsync(string? name, CancellationToken cancellationToken);
    Task<ShelterResponse> UpdateShelterAsync(Guid shelterId, UpdateShelterRequest request, CancellationToken cancellationToken);
    Task<ShelterResponse> GetShelterAsync(Guid shelterId, CancellationToken cancellationToken);
    Task<AnimalResponse> CreateAsync(CreateAnimalRequest request, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<AnimalResponse> UpdateAsync(Guid id, UpdateAnimalRequest request, Guid actorUserId, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<AnimalResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AnimalResponse>> ListAsync(AnimalSearchFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AnimalResponse>> ListNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken);
    Task<AnimalMediaResponse> AddMediaAsync(Guid animalId, MediaUpload upload, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<AnimalStatsResponse> GetStatsAsync(Guid animalId, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<AnimalResponse> MarkAdoptedAsync(Guid animalId, Guid actorUserId, CancellationToken cancellationToken);
}
