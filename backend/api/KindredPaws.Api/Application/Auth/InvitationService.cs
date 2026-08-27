using System.Security.Cryptography;
using System.Text;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Contracts;

namespace KindredPaws.Api.Application.Auth;

public sealed class InvitationService(IInvitationRepository repository, IEventPublisher eventPublisher) : IInvitationService
{
    public async Task<InvitationResponse> CreateAsync(string email, string fullName, string role, Guid createdByUserId, CancellationToken cancellationToken)
    {
        if (!Roles.All.Contains(role) || role == Roles.SuperAdministrator) throw new ArgumentException("Rol de invitación no permitido.");
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var invitation = new Invitation { Email = email.Trim().ToLowerInvariant(), FullName = fullName.Trim(), Role = role, TokenHash = Hash(rawToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), CreatedByUserId = createdByUserId };
        await repository.AddAsync(invitation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await eventPublisher.PublishAsync(new InvitationCreatedEvent(invitation.Id, invitation.Email, invitation.FullName, rawToken, invitation.ExpiresAt), cancellationToken);
        return ToResponse(invitation);
    }

    public async Task<IReadOnlyCollection<InvitationResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(ToResponse).ToArray();

    public async Task RevokeAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetAsync(invitationId, cancellationToken) ?? throw new KeyNotFoundException("Invitación no encontrada.");
        repository.Remove(invitation);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static InvitationResponse ToResponse(Invitation x) => new(x.Id, x.Email, x.FullName, x.Role, x.ExpiresAt);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
