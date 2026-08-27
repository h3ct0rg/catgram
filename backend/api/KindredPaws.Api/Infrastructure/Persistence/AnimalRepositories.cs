using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Shelters;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class ShelterRepository(AppDbContext db)
{
    public Task AddAsync(Shelter entity, CancellationToken ct) => db.Shelters.AddAsync(entity, ct).AsTask();
    public Task<Shelter?> GetAsync(Guid id, CancellationToken ct) => db.Shelters.Include(x => x.Animals).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<Shelter>> ListAsync(CancellationToken ct) => await db.Shelters.AsNoTracking().Include(x => x.Animals).OrderBy(x => x.Name).ToListAsync(ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class AnimalRepository(AppDbContext db)
{
    public Task AddAsync(Animal entity, CancellationToken ct) => db.Animals.AddAsync(entity, ct).AsTask();
    public Task<Animal?> GetAsync(Guid id, CancellationToken ct) => db.Animals.Include(x => x.Shelter).Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<Animal>> ListAsync(Guid? shelterId, CancellationToken ct) => await db.Animals.AsNoTracking().Include(x => x.Shelter).Include(x => x.Media).Where(x => shelterId == null || x.ShelterId == shelterId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
