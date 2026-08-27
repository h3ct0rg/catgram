namespace KindredPaws.Api.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken);
    Task<string> GetGoogleChallengeUrlAsync(string returnUrl, CancellationToken cancellationToken);
}
