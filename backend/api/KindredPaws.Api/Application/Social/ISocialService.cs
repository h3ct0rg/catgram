namespace KindredPaws.Api.Application.Social;

public interface ISocialService
{
    Task<PostResponse> CreatePostAsync(CreatePostRequest request, IReadOnlyCollection<MediaUpload> media, CancellationToken cancellationToken);
    Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken);
    Task HidePostAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostResponse>> GetFeedAsync(DateTimeOffset? before, int pageSize, CancellationToken cancellationToken);
    Task<StoryResponse> CreateStoryAsync(CreateStoryRequest request, MediaUpload media, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StoryResponse>> GetStoriesAsync(CancellationToken cancellationToken);
    Task RegisterStoryViewAsync(Guid id, string? anonymousKey, Guid? userId, CancellationToken cancellationToken);
}
