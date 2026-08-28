namespace KindredPaws.Api.Application.Social;

public sealed record CreateCommentRequest(string Body, Guid? ParentCommentId);
public sealed record CommentResponse(Guid Id, Guid PostId, Guid AuthorId, string AuthorName, Guid? ParentCommentId, string Body, DateTimeOffset CreatedAt, bool IsMine, int LikeCount, bool LikedByCurrentUser);
