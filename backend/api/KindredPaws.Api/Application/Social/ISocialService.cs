using KindredPaws.Api.Application.Animals;

namespace KindredPaws.Api.Application.Social;

public interface ISocialService
{
    Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid createdByUserId, Guid? actorShelterId, IReadOnlyCollection<MediaUpload> media, CancellationToken cancellationToken);
    Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid? actorShelterId, CancellationToken cancellationToken);
    Task HidePostAsync(Guid id, Guid actorUserId, Guid? actorShelterId, CancellationToken cancellationToken);
    Task<PostResponse> GetPostAsync(Guid id, Guid? currentUserId, CancellationToken cancellationToken);
    Task RegisterPostViewAsync(Guid id, CancellationToken cancellationToken);
    Task RegisterPostShareAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostResponse>> GetFeedAsync(DateTimeOffset? before, int skip, int pageSize, string sort, bool successStoriesOnly, Guid? currentUserId, CancellationToken cancellationToken);
    Task<StoryResponse> CreateStoryAsync(CreateStoryRequest request, Guid? actorShelterId, MediaUpload media, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StoryResponse>> GetStoriesAsync(CancellationToken cancellationToken);
    Task RegisterStoryViewAsync(Guid id, string? anonymousKey, Guid? userId, CancellationToken cancellationToken);
}
