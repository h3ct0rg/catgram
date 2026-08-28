using KindredPaws.Api.Domain.Adoption;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class AdoptionRequestRepository(AppDbContext db)
{
    public Task AddAsync(AdoptionRequest entity, CancellationToken ct) => db.AdoptionRequests.AddAsync(entity, ct).AsTask();
    public Task<AdoptionRequest?> GetAsync(Guid id, CancellationToken ct) => db.AdoptionRequests.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<AdoptionRequest>> ListAsync(AdoptionRequestStatus? status, Guid? animalId, Guid? shelterId, CancellationToken ct)
    {
        var query = db.AdoptionRequests.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (animalId.HasValue) query = query.Where(x => x.AnimalId == animalId.Value);
        if (shelterId.HasValue)
        {
            var shelterAnimalIds = db.Animals.Where(a => a.ShelterId == shelterId.Value).Select(a => a.Id);
            query = query.Where(x => shelterAnimalIds.Contains(x.AnimalId));
        }
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<AdoptionRequest>> ListByApplicantAsync(Guid applicantUserId, CancellationToken ct) =>
        await db.AdoptionRequests.AsNoTracking().Where(x => x.ApplicantUserId == applicantUserId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
