using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Shelters;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;

namespace KindredPaws.Api.Application.Animals;

public sealed class AnimalService(ShelterRepository shelters, AnimalRepository animals, IMediaStorage mediaStorage) : IAnimalService
{
    public async Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest r, CancellationToken ct)
    {
        var shelter = new Shelter { Name = r.Name.Trim(), Description = r.Description.Trim(), Address = r.Address.Trim(), City = r.City.Trim(), Country = r.Country.Trim(), Phone = r.Phone, WhatsApp = r.WhatsApp, Email = r.Email };
        await shelters.AddAsync(shelter, ct); await shelters.SaveAsync(ct); return ToResponse(shelter);
    }
    public async Task<IReadOnlyCollection<ShelterResponse>> ListSheltersAsync(CancellationToken ct) => (await shelters.ListAsync(ct)).Select(ToResponse).ToArray();
    public async Task<AnimalResponse> CreateAsync(CreateAnimalRequest r, CancellationToken ct)
    {
        var shelter = await shelters.GetAsync(r.ShelterId, ct) ?? throw new KeyNotFoundException("Refugio no encontrado.");
        var animal = new Animal { ShelterId = shelter.Id, Shelter = shelter, Name = r.Name.Trim(), Species = r.Species, Sex = r.Sex, Size = r.Size, AgeMonths = r.AgeMonths, Breed = r.Breed, Description = r.Description.Trim(), Location = r.Location };
        await animals.AddAsync(animal, ct); await animals.SaveAsync(ct); return await ToResponseAsync(animal, ct);
    }
    public async Task<AnimalResponse> UpdateAsync(Guid id, UpdateAnimalRequest r, CancellationToken ct)
    {
        var animal = await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        animal.Name = r.Name.Trim(); animal.Species = r.Species; animal.Sex = r.Sex; animal.Size = r.Size; animal.AgeMonths = r.AgeMonths; animal.Breed = r.Breed; animal.Description = r.Description.Trim(); animal.Location = r.Location; animal.AdoptionStatus = r.AdoptionStatus; animal.UpdatedAt = DateTimeOffset.UtcNow;
        await animals.SaveAsync(ct); return await ToResponseAsync(animal, ct);
    }
    public async Task<AnimalResponse> GetAsync(Guid id, CancellationToken ct) => await ToResponseAsync(await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Animal no encontrado."), ct);
    public async Task<IReadOnlyCollection<AnimalResponse>> ListAsync(Guid? shelterId, CancellationToken ct) => (await Task.WhenAll((await animals.ListAsync(shelterId, ct)).Select(x => ToResponseAsync(x, ct)))).ToArray();
    public async Task<AnimalMediaResponse> AddMediaAsync(Guid animalId, MediaUpload upload, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        if (upload.Length <= 0 || upload.Length > 20 * 1024 * 1024 || !new[] { "image/jpeg", "image/png", "image/webp", "video/mp4" }.Contains(upload.ContentType)) throw new ArgumentException("Archivo no permitido o excede 20 MB.");
        var key = $"animals/{animal.Id}/{Guid.NewGuid():N}-{Path.GetFileName(upload.FileName)}";
        await mediaStorage.PutAsync(key, upload.Content, upload.Length, upload.ContentType, ct);
        if (upload.IsPrimary) foreach (var media in animal.Media) media.IsPrimary = false;
        var entity = new AnimalMedia { AnimalId = animal.Id, ObjectKey = key, ContentType = upload.ContentType, SizeBytes = upload.Length, IsPrimary = upload.IsPrimary || animal.Media.Count == 0 };
        animal.Media.Add(entity); await animals.SaveAsync(ct); return new(entity.Id, await mediaStorage.GetUrlAsync(key, ct), entity.ContentType, entity.IsPrimary);
    }
    private static ShelterResponse ToResponse(Shelter x) => new(x.Id, x.Name, x.Description, x.Address, x.City, x.Country, x.Phone, x.WhatsApp, x.Email, x.Animals.Count);
    private async Task<AnimalResponse> ToResponseAsync(Animal x, CancellationToken ct) => new(x.Id, x.ShelterId, x.Shelter?.Name ?? "", x.Name, x.Species, x.Sex, x.Size, x.AgeMonths, x.Breed, x.Description, x.AdoptionStatus, x.Location, (await Task.WhenAll(x.Media.Select(async m => new AnimalMediaResponse(m.Id, await mediaStorage.GetUrlAsync(m.ObjectKey, ct), m.ContentType, m.IsPrimary)))).ToArray());
}
