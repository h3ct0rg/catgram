namespace KindredPaws.Api.Domain.Identity;

public sealed class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    /// <summary>For Administrador invitations: join this existing shelter (mutually exclusive with NewShelterName).</summary>
    public Guid? ShelterId { get; set; }
    /// <summary>For Administrador invitations: create a new shelter with this name on acceptance (mutually exclusive with ShelterId).</summary>
    public string? NewShelterName { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public Guid? UsedByUserId { get; set; }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
