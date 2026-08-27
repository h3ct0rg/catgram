namespace KindredPaws.Contracts;

public static class NotificationEventTypes
{
    public const string InvitationCreated = "identity.invitation-created";
    public const string LikeCreated = "social.like-created";
    public const string CommentCreated = "social.comment-created";
    public const string CommentReplyCreated = "social.comment-reply-created";
    public const string AdoptionStatusChanged = "animal.adoption-status-changed";
    public const string PostCreated = "social.post-created";
}

public sealed record EventEnvelope(string EventId, string Type, DateTimeOffset OccurredAt, object Payload);
public sealed record InvitationCreatedEvent(Guid InvitationId, string Email, string FullName, string Token, DateTimeOffset ExpiresAt);

// Each engagement event carries the recipient's email/name directly (like InvitationCreatedEvent already does)
// so the notification worker can send email without needing read access to the API's Identity tables.
public sealed record LikeCreatedEvent(Guid PostId, Guid RecipientUserId, string RecipientEmail, string RecipientName, Guid LikedByUserId, DateTimeOffset OccurredAt);
public sealed record CommentCreatedEvent(Guid CommentId, Guid PostId, Guid RecipientUserId, string RecipientEmail, string RecipientName, Guid AuthorUserId, string Excerpt);
public sealed record CommentReplyCreatedEvent(Guid CommentId, Guid ParentCommentId, Guid RecipientUserId, string RecipientEmail, string RecipientName, Guid AuthorUserId, string Excerpt);
public sealed record AdoptionStatusChangedEvent(Guid AnimalId, string AnimalName, string OldStatus, string NewStatus, Guid RecipientUserId, string RecipientEmail, string RecipientName);
public sealed record PostCreatedEvent(Guid PostId, Guid AnimalId, string AnimalName, Guid ShelterId, Guid RecipientUserId, string RecipientEmail, string RecipientName);
