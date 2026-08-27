using KindredPaws.Api.Application.Animals;

namespace KindredPaws.Api.Application.Social;

public interface ISocialService
{
    Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid createdByUserId, IReadOnlyCollection<MediaUpload> media, CancellationToken cancellationToken);
    Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken);
    Task HidePostAsync(Guid id, CancellationToken cancellationToken);
    Task<PostResponse> GetPostAsync(Guid id, Guid? currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostResponse>> GetFeedAsync(DateTimeOffset? before, int skip, int pageSize, string sort, Guid? currentUserId, CancellationToken cancellationToken);
    Task<StoryResponse> CreateStoryAsync(CreateStoryRequest request, MediaUpload media, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StoryResponse>> GetStoriesAsync(CancellationToken cancellationToken);
    Task RegisterStoryViewAsync(Guid id, string? anonymousKey, Guid? userId, CancellationToken cancellationToken);
}
