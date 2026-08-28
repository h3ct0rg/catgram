namespace KindredPaws.Api.Application.Auth;

public interface IInvitationService
{
    Task<InvitationResponse> CreateAsync(string email, string fullName, string role, Guid? shelterId, string? newShelterName, Guid createdByUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<InvitationResponse>> ListAsync(CancellationToken cancellationToken);
    Task RevokeAsync(Guid invitationId, CancellationToken cancellationToken);
    Task<InvitationResponse> ResendAsync(Guid invitationId, CancellationToken cancellationToken);
}
