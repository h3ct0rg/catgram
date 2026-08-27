using KindredPaws.Api.Domain.Identity;

namespace KindredPaws.Api.Infrastructure.Persistence;

public interface IInvitationRepository
{
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken);
    Task<Invitation?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Invitation?> FindUsableByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Invitation>> ListAsync(CancellationToken cancellationToken);
    void Remove(Invitation invitation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
