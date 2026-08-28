using KindredPaws.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class RefreshTokenRepository(AppDbContext db)
{
    public Task AddAsync(RefreshToken entity, CancellationToken ct) => db.RefreshTokens.AddAsync(entity, ct).AsTask();

    public Task<RefreshToken?> FindActiveByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, ct);

    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
