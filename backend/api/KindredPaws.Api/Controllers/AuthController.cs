using KindredPaws.Api.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

    [HttpGet("google/challenge")]
    [AllowAnonymous]
    public IActionResult GoogleChallenge([FromQuery] string? invitationToken, [FromQuery] string? returnUrl)
    {
        var properties = new AuthenticationProperties { RedirectUri = "/api/v1/auth/google/callback" };
        if (!string.IsNullOrWhiteSpace(invitationToken)) properties.Items["invitation_token"] = invitationToken;
        if (!string.IsNullOrWhiteSpace(returnUrl)) properties.Items["return_url"] = returnUrl;
        return Challenge(properties, "Google");
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GoogleCallback(CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal is null) return Unauthorized();
        var subject = result.Principal.FindFirst("sub")?.Value;
        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        string? token = result.Properties?.Items.TryGetValue("invitation_token", out var invitationToken) == true ? invitationToken : null;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)) return Forbid();
        return Ok(await authService.AcceptInvitationAsync(new AcceptInvitationRequest(token, subject, email), cancellationToken));
    }

}
