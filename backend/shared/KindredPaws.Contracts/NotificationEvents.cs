namespace KindredPaws.Contracts;

public static class NotificationEventTypes
{
    public const string InvitationCreated = "identity.invitation-created";
}

public sealed record EventEnvelope(string EventId, string Type, DateTimeOffset OccurredAt, object Payload);
public sealed record InvitationCreatedEvent(Guid InvitationId, string Email, string FullName, string Token, DateTimeOffset ExpiresAt);
