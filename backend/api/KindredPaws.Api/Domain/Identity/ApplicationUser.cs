using Microsoft.AspNetCore.Identity;

namespace KindredPaws.Api.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    /// <summary>Shelter this user administers (Administrador role only). Several admins can share a shelter.</summary>
    public Guid? ShelterId { get; set; }
}
