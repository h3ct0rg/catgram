using KindredPaws.Api.Domain.Adoption;

namespace KindredPaws.Api.Application.Adoption;

public interface IAdoptionService
{
    Task<AdoptionRequestResponse> CreateAsync(Guid animalId, Guid applicantUserId, CreateAdoptionRequestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdoptionRequestResponse>> ListAsync(AdoptionRequestStatus? status, Guid? animalId, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdoptionRequestResponse>> ListMineAsync(Guid applicantUserId, CancellationToken cancellationToken);
    Task<AdoptionRequestResponse> UpdateStatusAsync(Guid id, AdoptionRequestStatus status, string? reviewNotes, Guid actorUserId, Guid? actorShelterId, CancellationToken cancellationToken);
}
