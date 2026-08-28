using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Shelters;
using KindredPaws.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KindredPaws.Api.Application.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IInvitationRepository invitations,
    ShelterRepository shelters,
    IOptions<JwtOptions> jwtOptions,
    IOptions<GoogleAuthOptions> googleOptions) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName)
                   ?? throw new UnauthorizedAccessException("Credenciales inválidas.");
        if (!user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        return await CreateTokenAsync(user);
    }

    public async Task<AuthResponse> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation = await invitations.FindUsableByTokenHashAsync(Hash(request.Token), cancellationToken)
            ?? throw new InvalidOperationException("La invitación no es válida o ya expiró.");
        if (!string.Equals(invitation.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El correo no coincide con la invitación.");

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = invitation.FullName,
                EmailConfirmed = true,
                IsActive = true
            };
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
        }

        await userManager.AddLoginAsync(user, new UserLoginInfo("Google", request.GoogleSubject, "Google"));
        await userManager.AddToRoleAsync(user, invitation.Role);

        if (invitation.Role == Roles.Administrator && user.ShelterId is null)
        {
            if (invitation.ShelterId.HasValue)
            {
                user.ShelterId = invitation.ShelterId;
            }
            else if (!string.IsNullOrWhiteSpace(invitation.NewShelterName))
            {
                var shelter = new Shelter { Name = invitation.NewShelterName, Description = "", Address = "", City = "", Country = "" };
                await shelters.AddAsync(shelter, cancellationToken);
                await shelters.SaveAsync(cancellationToken);
                user.ShelterId = shelter.Id;
            }
            await userManager.UpdateAsync(user);
        }

        invitation.UsedAt = DateTimeOffset.UtcNow;
        invitation.UsedByUserId = user.Id;
        await invitations.SaveChangesAsync(cancellationToken);
        return await CreateTokenAsync(user);
    }

    public async Task<AuthResponse> GoogleLoginAsync(string idToken, string? invitationToken, CancellationToken cancellationToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleOptions.Value.ClientId]
            });
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Token de Google inválido.");
        }

        if (!string.IsNullOrWhiteSpace(invitationToken))
            return await AcceptInvitationAsync(new AcceptInvitationRequest(invitationToken, payload.Subject, payload.Email), cancellationToken);

        var user = await userManager.FindByLoginAsync("Google", payload.Subject)
            ?? throw new UnauthorizedAccessException("Esta cuenta de Google no está registrada. Solicita una invitación.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Credenciales inválidas.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        return await CreateTokenAsync(user);
    }

    private async Task<AuthResponse> CreateTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (user.ShelterId.HasValue) claims.Add(new Claim("shelter_id", user.ShelterId.Value.ToString()));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: jwtOptions.Value.Issuer, claims: claims, expires: expires.UtcDateTime, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.UserName!, [.. roles], user.ShelterId, user.MustChangePassword);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class JwtOptions
{
    public string Key { get; set; } = "development-only-change-this-key-32-chars";
    public string Issuer { get; set; } = "kindred-paws-api";
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
}
