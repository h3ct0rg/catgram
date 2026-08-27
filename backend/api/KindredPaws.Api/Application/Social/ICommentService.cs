namespace KindredPaws.Api.Application.Social;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CommentResponse>> ListAsync(Guid postId, Guid? currentUserId, CancellationToken cancellationToken);
    Task DeleteOwnAsync(Guid commentId, Guid userId, CancellationToken cancellationToken);
    Task HideAsync(Guid commentId, CancellationToken cancellationToken);
}
