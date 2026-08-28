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

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => NoContent();

    [HttpPost("google-login")]
    [AllowAnonymous]
    public Task<AuthResponse> GoogleLogin(GoogleLoginRequest request, CancellationToken cancellationToken) =>
        authService.GoogleLoginAsync(request.IdToken, request.InvitationToken, cancellationToken);
}
