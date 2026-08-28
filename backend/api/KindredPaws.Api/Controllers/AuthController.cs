using KindredPaws.Api.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KindredPaws.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken) => authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request.RefreshToken, cancellationToken);

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        // Anonymous on purpose: the access token may already be expired right when the user logs out —
        // the refresh token itself is the only credential this needs to revoke it.
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    public Task<AuthResponse> GoogleLogin(GoogleLoginRequest request, CancellationToken cancellationToken) =>
        authService.GoogleLoginAsync(request.IdToken, request.InvitationToken, cancellationToken);
}
