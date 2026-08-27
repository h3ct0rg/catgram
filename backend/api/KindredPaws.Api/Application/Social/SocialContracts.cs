using KindredPaws.Api.Application.Animals;

namespace KindredPaws.Api.Application.Social;

public sealed record CreatePostRequest(Guid ShelterId, Guid AnimalId, string Caption, string? Location, string? Hashtags, bool IsFeatured);
public sealed record UpdatePostRequest(string Caption, string? Location, string? Hashtags, bool IsFeatured);
public sealed record CreateStoryRequest(Guid ShelterId, Guid AnimalId, string Caption);
public sealed record PostResponse(Guid Id, Guid ShelterId, Guid AnimalId, string Caption, string? Location, string? Hashtags, bool IsFeatured, DateTimeOffset CreatedAt, IReadOnlyCollection<AnimalMediaResponse> Media);
public sealed record StoryResponse(Guid Id, Guid ShelterId, Guid AnimalId, string Caption, string MediaUrl, string ContentType, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int Views);
