using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Domain.Shelters;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;
using KindredPaws.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Animals;

public sealed class AnimalService(
    ShelterRepository shelters,
    AnimalRepository animals,
    FollowRepository follows,
    SocialRepository posts,
    LikeRepository likes,
    CommentRepository comments,
    IMediaStorage mediaStorage,
    IThumbnailGenerator thumbnailGenerator,
    UserManager<ApplicationUser> userManager,
    INotificationService notifications,
    IEventPublisher eventPublisher,
    IAuditService audit) : IAnimalService
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
    public async Task<AnimalResponse> UpdateAsync(Guid id, UpdateAnimalRequest r, Guid actorUserId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        var previousStatus = animal.AdoptionStatus;
        animal.Name = r.Name.Trim(); animal.Species = r.Species; animal.Sex = r.Sex; animal.Size = r.Size; animal.AgeMonths = r.AgeMonths; animal.Breed = r.Breed; animal.Description = r.Description.Trim(); animal.Location = r.Location; animal.AdoptionStatus = r.AdoptionStatus; animal.UpdatedAt = DateTimeOffset.UtcNow;
        await animals.SaveAsync(ct);
        if (previousStatus != animal.AdoptionStatus)
        {
            await audit.RecordAsync(actorUserId, AuditAction.AdoptionStatusChanged, "Animal", id, $"{previousStatus} -> {animal.AdoptionStatus}", ct);
            await NotifyAdoptionStatusChangedAsync(animal, previousStatus, ct);
        }
        return await ToResponseAsync(animal, ct);
    }
    private async Task NotifyAdoptionStatusChangedAsync(Animal animal, AdoptionStatus previousStatus, CancellationToken ct)
    {
        var followerIds = await follows.ListFollowerIdsAsync(animal.Id, ct);
        if (followerIds.Count == 0) return;
        var followers = await userManager.Users.Where(u => followerIds.Contains(u.Id)).ToListAsync(ct);
        foreach (var follower in followers)
        {
            await notifications.CreateAsync(follower.Id, NotificationType.AdoptionStatusChanged, "Actualización de adopción", $"{animal.Name} cambió su estado a {animal.AdoptionStatus}.", $"/animals/{animal.Id}", animal.Id, ct);
            await eventPublisher.PublishAsync(new AdoptionStatusChangedEvent(animal.Id, animal.Name, previousStatus.ToString(), animal.AdoptionStatus.ToString(), follower.Id, follower.Email ?? string.Empty, follower.FullName), ct);
        }
    }
    public async Task<AnimalResponse> GetAsync(Guid id, CancellationToken ct) => await ToResponseAsync(await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Animal no encontrado."), ct);
    public async Task<IReadOnlyCollection<AnimalResponse>> ListAsync(Guid? shelterId, CancellationToken ct) => (await Task.WhenAll((await animals.ListAsync(shelterId, ct)).Select(x => ToResponseAsync(x, ct)))).ToArray();
    public async Task<AnimalMediaResponse> AddMediaAsync(Guid animalId, MediaUpload upload, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        if (upload.Length <= 0 || upload.Length > 20 * 1024 * 1024 || !new[] { "image/jpeg", "image/png", "image/webp", "video/mp4" }.Contains(upload.ContentType)) throw new ArgumentException("Archivo no permitido o excede 20 MB.");
        using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        var key = $"animals/{animal.Id}/{Guid.NewGuid():N}-{Path.GetFileName(upload.FileName)}";
        buffer.Position = 0;
        await mediaStorage.PutAsync(key, buffer, upload.Length, upload.ContentType, ct);
        var thumbnailKey = await TryGenerateThumbnailAsync(buffer, upload.ContentType, $"animals/{animal.Id}", ct);
        if (upload.IsPrimary) foreach (var media in animal.Media) media.IsPrimary = false;
        var entity = new AnimalMedia { AnimalId = animal.Id, ObjectKey = key, ThumbnailObjectKey = thumbnailKey, ContentType = upload.ContentType, SizeBytes = upload.Length, IsPrimary = upload.IsPrimary || animal.Media.Count == 0 };
        animal.Media.Add(entity); await animals.SaveAsync(ct); return await ToMediaResponseAsync(entity, ct);
    }
    private async Task<string?> TryGenerateThumbnailAsync(MemoryStream buffer, string contentType, string keyPrefix, CancellationToken ct)
    {
        if (!thumbnailGenerator.CanGenerate(contentType)) return null;
        buffer.Position = 0;
        var thumbnail = await thumbnailGenerator.GenerateAsync(buffer, ct);
        if (thumbnail is null) return null;
        var thumbnailKey = $"{keyPrefix}/{Guid.NewGuid():N}-thumb.webp";
        await mediaStorage.PutAsync(thumbnailKey, thumbnail.Value.Content, thumbnail.Value.Length, thumbnail.Value.ContentType, ct);
        await thumbnail.Value.Content.DisposeAsync();
        return thumbnailKey;
    }
    public async Task<AnimalStatsResponse> GetStatsAsync(Guid animalId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        var (postCount, totalViews, totalShares, postIds) = await posts.GetAnimalPostStatsAsync(animalId, ct);
        var totalLikes = postIds.Count == 0 ? 0 : (await likes.CountManyAsync(postIds, ct)).Values.Sum();
        var totalComments = postIds.Count == 0 ? 0 : (await comments.CountManyAsync(postIds, ct)).Values.Sum();
        var followerCount = await follows.CountAsync(animalId, ct);
        return new AnimalStatsResponse(animal.Id, animal.Name, animal.AdoptionStatus, postCount, totalLikes, totalComments, totalViews, totalShares, followerCount);
    }

    private static ShelterResponse ToResponse(Shelter x) => new(x.Id, x.Name, x.Description, x.Address, x.City, x.Country, x.Phone, x.WhatsApp, x.Email, x.Animals.Count);
    private async Task<AnimalMediaResponse> ToMediaResponseAsync(AnimalMedia m, CancellationToken ct) => new(m.Id, await mediaStorage.GetUrlAsync(m.ObjectKey, ct), m.ThumbnailObjectKey is null ? null : await mediaStorage.GetUrlAsync(m.ThumbnailObjectKey, ct), m.ContentType, m.IsPrimary);
    private async Task<AnimalResponse> ToResponseAsync(Animal x, CancellationToken ct) => new(x.Id, x.ShelterId, x.Shelter?.Name ?? "", x.Name, x.Species, x.Sex, x.Size, x.AgeMonths, x.Breed, x.Description, x.AdoptionStatus, x.Location, (await Task.WhenAll(x.Media.Select(m => ToMediaResponseAsync(m, ct)))).ToArray());
}
