using KindredPaws.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class InvitationRepository(AppDbContext db) : IInvitationRepository
{
    public Task AddAsync(Invitation invitation, CancellationToken cancellationToken) => db.Invitations.AddAsync(invitation, cancellationToken).AsTask();

    public Task<Invitation?> GetAsync(Guid id, CancellationToken cancellationToken) => db.Invitations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Invitation?> FindUsableByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Invitations.SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.UsedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);

    public async Task<IReadOnlyCollection<Invitation>> ListAsync(CancellationToken cancellationToken) =>
        await db.Invitations.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public void Remove(Invitation invitation) => db.Invitations.Remove(invitation);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
