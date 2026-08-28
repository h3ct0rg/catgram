namespace KindredPaws.Api.Application.Auth;

public sealed record LoginRequest(string UserName, string Password);
public sealed record AcceptInvitationRequest(string Token, string GoogleSubject, string Email);
public sealed record InvitationResponse(Guid Id, string Email, string FullName, string Role, Guid? ShelterId, string? ShelterName, string? NewShelterName, DateTimeOffset ExpiresAt, string Status);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, string UserName, string[] Roles, Guid? ShelterId, bool MustChangePassword);
public sealed record UserSummary(Guid Id, string UserName, string Email, string FullName, bool IsActive, string[] Roles);
