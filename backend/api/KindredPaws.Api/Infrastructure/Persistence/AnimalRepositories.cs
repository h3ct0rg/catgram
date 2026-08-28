using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Shelters;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class ShelterRepository(AppDbContext db)
{
    public Task AddAsync(Shelter entity, CancellationToken ct) => db.Shelters.AddAsync(entity, ct).AsTask();
    public Task<Shelter?> GetAsync(Guid id, CancellationToken ct) => db.Shelters.Include(x => x.Animals).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<Shelter>> ListAsync(string? name, CancellationToken ct)
    {
        var query = db.Shelters.AsNoTracking().Include(x => x.Animals).AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name}%"));
        return await query.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Shelter>> ListWithCoordinatesAsync(CancellationToken ct) =>
        await db.Shelters.AsNoTracking().Where(x => x.Latitude != null && x.Longitude != null).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class AnimalRepository(AppDbContext db)
{
    public Task AddAsync(Animal entity, CancellationToken ct) => db.Animals.AddAsync(entity, ct).AsTask();
    public Task<Animal?> GetAsync(Guid id, CancellationToken ct) => db.Animals.Include(x => x.Shelter).Include(x => x.Media).SingleOrDefaultAsync(x => x.Id == id, ct);

    // AnimalMedia.Id is a client-generated GUID (set in C#, not DB-generated). If a new AnimalMedia is
    // only ever added via the Animal.Media navigation collection (animal.Media.Add(...)), EF Core's
    // change tracker discovers it during DetectChanges rather than via an explicit Add — and because its
    // key is already non-default, EF assumes it's an *existing* row and marks it Modified instead of
    // Added, generating an UPDATE that matches zero rows (DbUpdateConcurrencyException). Adding it
    // directly to its own DbSet here removes the ambiguity: it's unconditionally tracked as Added.
    public Task AddMediaAsync(AnimalMedia entity, CancellationToken ct) => db.AnimalMedia.AddAsync(entity, ct).AsTask();

    public async Task<IReadOnlyCollection<Animal>> ListAsync(AnimalSearchFilter filter, CancellationToken ct)
    {
        var query = db.Animals.AsNoTracking().Include(x => x.Shelter).Include(x => x.Media).AsQueryable();
        if (filter.ShelterId.HasValue) query = query.Where(x => x.ShelterId == filter.ShelterId);
        if (!string.IsNullOrWhiteSpace(filter.Name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{filter.Name}%"));
        if (filter.Species.HasValue) query = query.Where(x => x.Species == filter.Species);
        if (filter.Sex.HasValue) query = query.Where(x => x.Sex == filter.Sex);
        if (filter.Size.HasValue) query = query.Where(x => x.Size == filter.Size);
        if (!string.IsNullOrWhiteSpace(filter.Breed)) query = query.Where(x => x.Breed != null && EF.Functions.ILike(x.Breed, $"%{filter.Breed}%"));
        if (!string.IsNullOrWhiteSpace(filter.Location)) query = query.Where(x => x.Location != null && EF.Functions.ILike(x.Location, $"%{filter.Location}%"));
        if (filter.AdoptionStatus.HasValue) query = query.Where(x => x.AdoptionStatus == filter.AdoptionStatus);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public void Remove(Animal entity) => db.Animals.Remove(entity);
}
