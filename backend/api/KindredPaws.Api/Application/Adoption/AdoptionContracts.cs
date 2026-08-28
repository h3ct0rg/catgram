using KindredPaws.Api.Domain.Adoption;

namespace KindredPaws.Api.Application.Adoption;

public sealed record CreateAdoptionRequestRequest(Dictionary<string, string> Answers);
public sealed record UpdateAdoptionRequestStatusRequest(AdoptionRequestStatus Status, string? ReviewNotes);
public sealed record AdoptionRequestResponse(
    Guid Id,
    Guid AnimalId,
    string AnimalName,
    Guid ApplicantUserId,
    string ApplicantUserName,
    AdoptionRequestStatus Status,
    Dictionary<string, string> Answers,
    string? ReviewNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
