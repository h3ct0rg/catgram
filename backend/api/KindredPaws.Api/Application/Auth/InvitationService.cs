using System.Security.Cryptography;
using System.Text;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Contracts;

namespace KindredPaws.Api.Application.Auth;

public sealed class InvitationService(IInvitationRepository repository, ShelterRepository shelters, IEventPublisher eventPublisher) : IInvitationService
{
    public async Task<InvitationResponse> CreateAsync(string email, string fullName, string role, Guid? shelterId, string? newShelterName, Guid createdByUserId, CancellationToken cancellationToken)
    {
        if (!Roles.All.Contains(role) || role == Roles.SuperAdministrator) throw new ArgumentException("Rol de invitación no permitido.");

        if (role == Roles.Administrator)
        {
            var hasExisting = shelterId.HasValue;
            var hasNew = !string.IsNullOrWhiteSpace(newShelterName);
            if (hasExisting == hasNew) throw new ArgumentException("Para invitar a un Administrador, indica el refugio existente al que se une o el nombre de un refugio nuevo, pero no ambos.");
            if (hasExisting && await shelters.GetAsync(shelterId!.Value, cancellationToken) is null) throw new KeyNotFoundException("Refugio no encontrado.");
        }
        else if (shelterId.HasValue || !string.IsNullOrWhiteSpace(newShelterName))
        {
            throw new ArgumentException("Solo las invitaciones de rol Administrador pueden asociarse a un refugio.");
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var invitation = new Invitation
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            Role = role,
            ShelterId = shelterId,
            NewShelterName = newShelterName?.Trim(),
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByUserId = createdByUserId
        };
        await repository.AddAsync(invitation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await eventPublisher.PublishAsync(new InvitationCreatedEvent(invitation.Id, invitation.Email, invitation.FullName, rawToken, invitation.ExpiresAt), cancellationToken);
        var shelterName = shelterId.HasValue ? (await shelters.GetAsync(shelterId.Value, cancellationToken))?.Name : null;
        return ToResponse(invitation, DateTimeOffset.UtcNow, shelterName);
    }

    public async Task<IReadOnlyCollection<InvitationResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var invitations = await repository.ListAsync(cancellationToken);
        var shelterNames = (await shelters.ListAsync(null, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var now = DateTimeOffset.UtcNow;
        return invitations.Select(x => ToResponse(x, now, shelterNames)).ToArray();
    }

    public async Task RevokeAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetAsync(invitationId, cancellationToken) ?? throw new KeyNotFoundException("Invitación no encontrada.");
        repository.Remove(invitation);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvitationResponse> ResendAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetAsync(invitationId, cancellationToken) ?? throw new KeyNotFoundException("Invitación no encontrada.");
        if (invitation.UsedAt is not null) throw new ArgumentException("La invitación ya fue aceptada, no se puede reenviar.");

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        invitation.TokenHash = Hash(rawToken);
        invitation.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        await repository.SaveChangesAsync(cancellationToken);
        await eventPublisher.PublishAsync(new InvitationCreatedEvent(invitation.Id, invitation.Email, invitation.FullName, rawToken, invitation.ExpiresAt), cancellationToken);

        var shelterName = invitation.ShelterId.HasValue ? (await shelters.GetAsync(invitation.ShelterId.Value, cancellationToken))?.Name : null;
        return ToResponse(invitation, DateTimeOffset.UtcNow, shelterName);
    }

    private static InvitationResponse ToResponse(Invitation x, DateTimeOffset now, IReadOnlyDictionary<Guid, string> shelterNames) =>
        ToResponse(x, now, x.ShelterId.HasValue && shelterNames.TryGetValue(x.ShelterId.Value, out var name) ? name : null);

    private static InvitationResponse ToResponse(Invitation x, DateTimeOffset now, string? shelterName) =>
        new(x.Id, x.Email, x.FullName, x.Role, x.ShelterId, shelterName, x.NewShelterName, x.ExpiresAt, ComputeStatus(x, now));

    private static string ComputeStatus(Invitation x, DateTimeOffset now) =>
        x.UsedAt is not null ? "Aceptada" : x.ExpiresAt <= now ? "Expirada" : "Pendiente";

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
