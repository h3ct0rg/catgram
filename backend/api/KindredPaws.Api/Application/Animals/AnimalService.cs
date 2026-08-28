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
    AdoptionRequestRepository adoptionRequests,
    IMinioService minioService,
    UserManager<ApplicationUser> userManager,
    INotificationService notifications,
    IEventPublisher eventPublisher,
    IAuditService audit,
    ILogger<AnimalService> logger) : IAnimalService
{
    // "image/jpg" isn't a registered MIME type but some browsers/OS combinations report it anyway for
    // .jpg files instead of the standard "image/jpeg" — accept both rather than silently rejecting them.
    public async Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest r, CancellationToken ct)
    {
        var shelter = new Shelter { Name = r.Name.Trim(), Description = r.Description.Trim(), Address = r.Address.Trim(), City = r.City.Trim(), Country = r.Country.Trim(), Phone = r.Phone, WhatsApp = r.WhatsApp, Email = r.Email, Latitude = r.Latitude, Longitude = r.Longitude };
        await shelters.AddAsync(shelter, ct);
        await SaveSheltersAsync(ct, "crear refugio", shelter.Id);
        logger.LogInformation("Shelter {ShelterId} ({ShelterName}) created.", shelter.Id, shelter.Name);
        return ToResponse(shelter);
    }

    public async Task<IReadOnlyCollection<ShelterResponse>> ListSheltersAsync(string? name, CancellationToken ct) => (await shelters.ListAsync(name, ct)).Select(ToResponse).ToArray();

    public async Task<ShelterResponse> UpdateShelterAsync(Guid shelterId, UpdateShelterRequest r, CancellationToken ct)
    {
        var shelter = await shelters.GetAsync(shelterId, ct) ?? throw new KeyNotFoundException("Refugio no encontrado.");
        shelter.Name = r.Name.Trim(); shelter.Description = r.Description.Trim(); shelter.Address = r.Address.Trim(); shelter.City = r.City.Trim(); shelter.Country = r.Country.Trim();
        shelter.Phone = r.Phone; shelter.WhatsApp = r.WhatsApp; shelter.Email = r.Email; shelter.Latitude = r.Latitude; shelter.Longitude = r.Longitude;
        await SaveSheltersAsync(ct, "actualizar refugio", shelter.Id);
        logger.LogInformation("Shelter {ShelterId} updated.", shelter.Id);
        return ToResponse(shelter);
    }

    public async Task<ShelterResponse> GetShelterAsync(Guid shelterId, CancellationToken ct) =>
        ToResponse(await shelters.GetAsync(shelterId, ct) ?? throw new KeyNotFoundException("Refugio no encontrado."));

    public async Task<AnimalResponse> CreateAsync(CreateAnimalRequest r, Guid? actorShelterId, CancellationToken ct)
    {
        var effectiveShelterId = actorShelterId ?? r.ShelterId;
        var shelter = await shelters.GetAsync(effectiveShelterId, ct) ?? throw new KeyNotFoundException("Refugio no encontrado.");
        var animal = new Animal { ShelterId = shelter.Id, Shelter = shelter, Name = r.Name.Trim(), Species = r.Species, Sex = r.Sex, Size = r.Size, AgeMonths = r.AgeMonths, Breed = r.Breed, Description = r.Description.Trim(), Location = r.Location };
        await animals.AddAsync(animal, ct);
        await SaveAnimalsAsync(ct, "crear animal", animal.Id);
        logger.LogInformation("Animal {AnimalId} ({AnimalName}) created for shelter {ShelterId}.", animal.Id, animal.Name, animal.ShelterId);
        return await ToResponseAsync(animal, ct);
    }

    public async Task<AnimalResponse> UpdateAsync(Guid id, UpdateAnimalRequest r, Guid actorUserId, Guid? actorShelterId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Mascota no encontrada.");
        EnsureShelterAccess(animal, actorShelterId);
        var previousStatus = animal.AdoptionStatus;
        animal.Name = r.Name.Trim(); animal.Species = r.Species; animal.Sex = r.Sex; animal.Size = r.Size; animal.AgeMonths = r.AgeMonths; animal.Breed = r.Breed; animal.Description = r.Description.Trim(); animal.Location = r.Location; animal.AdoptionStatus = r.AdoptionStatus; animal.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAnimalsAsync(ct, "actualizar animal", animal.Id);
        logger.LogInformation("Animal {AnimalId} updated.", animal.Id);
        if (previousStatus != animal.AdoptionStatus)
        {
            await audit.RecordAsync(actorUserId, AuditAction.AdoptionStatusChanged, "Animal", id, $"{previousStatus} -> {animal.AdoptionStatus}", ct);
            await NotifyAdoptionStatusChangedAsync(animal, previousStatus, ct);
        }
        return await ToResponseAsync(animal, ct);
    }

    private async Task NotifyAdoptionStatusChangedAsync(Animal animal, AdoptionStatus previousStatus, CancellationToken ct)
    {
        // Best-effort: the adoption status change itself is already committed by the time this runs.
        // A follower/notification failure here must never surface as a failed request for an update
        // that actually succeeded.
        try
        {
            var followerIds = await follows.ListFollowerIdsAsync(animal.Id, ct);
            if (followerIds.Count == 0) return;
            var followers = await userManager.Users.Where(u => followerIds.Contains(u.Id)).ToListAsync(ct);
            foreach (var follower in followers)
            {
                await notifications.CreateAsync(follower.Id, NotificationType.AdoptionStatusChanged, "Actualización de adopción", $"{animal.Name} cambió su estado a {animal.AdoptionStatus}.", $"/animals/{animal.Id}", animal.Id, ct);
                await eventPublisher.PublishAsync(new AdoptionStatusChangedEvent(animal.Id, animal.Name, previousStatus.ToString(), animal.AdoptionStatus.ToString(), follower.Id, follower.Email ?? string.Empty, follower.FullName), ct);
            }
            logger.LogInformation("Notified {FollowerCount} follower(s) of animal {AnimalId} status change {Previous} -> {Current}.", followers.Count, animal.Id, previousStatus, animal.AdoptionStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify followers of animal {AnimalId} status change {Previous} -> {Current}; the status change itself was already saved.", animal.Id, previousStatus, animal.AdoptionStatus);
        }
    }

    public async Task<AnimalResponse> GetAsync(Guid id, CancellationToken ct) => await ToResponseAsync(await animals.GetAsync(id, ct) ?? throw new KeyNotFoundException("Mascota no encontrada."), ct);
    public async Task<IReadOnlyCollection<AnimalResponse>> ListAsync(AnimalSearchFilter filter, CancellationToken ct) => (await Task.WhenAll((await animals.ListAsync(filter, ct)).Select(x => ToResponseAsync(x, ct)))).ToArray();

    public async Task<IReadOnlyCollection<AnimalResponse>> ListNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken ct)
    {
        var sheltersWithCoords = await shelters.ListWithCoordinatesAsync(ct);
        var nearbyShelterIds = sheltersWithCoords
            .Where(s => HaversineKm(latitude, longitude, s.Latitude!.Value, s.Longitude!.Value) <= radiusKm)
            .Select(s => s.Id)
            .ToHashSet();
        if (nearbyShelterIds.Count == 0) return [];

        var available = await animals.ListAsync(new AnimalSearchFilter(null, null, null, null, null, null, null, AdoptionStatus.Available), ct);
        var candidates = available.Where(x => nearbyShelterIds.Contains(x.ShelterId));
        return await Task.WhenAll(candidates.Select(x => ToResponseAsync(x, ct)));
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>actorShelterId is null for SuperAdmin (unrestricted); Administradores are confined to their own shelter's animals.</summary>
    private static void EnsureShelterAccess(Animal animal, Guid? actorShelterId)
    {
        if (actorShelterId.HasValue && animal.ShelterId != actorShelterId.Value)
            throw new UnauthorizedAccessException("No puedes gestionar mascotas de otro refugio.");
    }

    public async Task<AnimalResponse> MarkAdoptedAsync(Guid animalId, Guid actorUserId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Mascota no encontrada.");
        var previousStatus = animal.AdoptionStatus;
        if (previousStatus != AdoptionStatus.Adopted)
        {
            animal.AdoptionStatus = AdoptionStatus.Adopted;
            animal.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveAnimalsAsync(ct, "marcar animal como adoptado", animal.Id);
            await audit.RecordAsync(actorUserId, AuditAction.AdoptionStatusChanged, "Animal", animalId, $"{previousStatus} -> Adopted (solicitud de adopción completada)", ct);
            await NotifyAdoptionStatusChangedAsync(animal, previousStatus, ct);
            logger.LogInformation("Animal {AnimalId} marked as adopted (was {Previous}).", animal.Id, previousStatus);
        }
        return await ToResponseAsync(animal, ct);
    }

    public async Task DeleteAsync(Guid animalId, Guid? actorShelterId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Mascota no encontrada.");
        EnsureShelterAccess(animal, actorShelterId);

        // Post/AdoptionRequest.AnimalId are required FKs with no explicit OnDelete configured, so EF's
        // convention default (Cascade) would silently wipe a pet's entire post/like/comment/adoption
        // history on delete. Block it instead — an admin has to clear those first. Follows are low-stakes
        // (just subscriptions) and are left to cascade away.
        var (postCount, _, _, _) = await posts.GetAnimalPostStatsAsync(animalId, ct);
        if (postCount > 0)
        {
            logger.LogWarning("Refused to delete animal {AnimalId}: it has {PostCount} post(s).", animalId, postCount);
            throw new InvalidOperationException("No puedes eliminar una mascota con publicaciones. Elimina sus publicaciones primero.");
        }
        var existingAdoptionRequests = await adoptionRequests.ListAsync(null, animalId, null, ct);
        if (existingAdoptionRequests.Count > 0)
        {
            logger.LogWarning("Refused to delete animal {AnimalId}: it has {RequestCount} adoption request(s).", animalId, existingAdoptionRequests.Count);
            throw new InvalidOperationException("No puedes eliminar una mascota con solicitudes de adopción registradas.");
        }

        animals.Remove(animal);
        await SaveAnimalsAsync(ct, "eliminar mascota", animal.Id);
        logger.LogInformation("Animal {AnimalId} ({AnimalName}) deleted.", animal.Id, animal.Name);
    }

    public async Task<AnimalMediaResponse> AddMediaAsync(Guid animalId, MediaUpload upload, Guid? actorShelterId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Mascota no encontrada.");
        EnsureShelterAccess(animal, actorShelterId);
        var existingMedia = string.Join("; ", animal.Media.Select(m => $"{m.Id}(isPrimary={m.IsPrimary})"));
        logger.LogInformation("Adding media to animal {AnimalId}: {ContentType}, {Length} bytes, isPrimary={IsPrimary}, existing media=[{ExistingMedia}].",
            animal.Id, upload.ContentType, upload.Length, upload.IsPrimary, existingMedia);

        if (upload.Length <= 0 || upload.Length > 50 * 1024 * 1024)
        {
            logger.LogWarning("Rejected media upload for animal {AnimalId}: size {Length} bytes.", animal.Id, upload.Length);
            throw new ArgumentException("El archivo excede 50 MB.");
        }

        try
        {
            var stored = await minioService.StoreAsync($"animals/{animal.Id}", upload.FileName, upload.ContentType, upload.Length, upload.Content, withThumbnail: true, ct);
            if (upload.IsPrimary) foreach (var media in animal.Media) media.IsPrimary = false;
            var entity = new AnimalMedia { AnimalId = animal.Id, ObjectKey = stored.ObjectKey, ThumbnailObjectKey = stored.ThumbnailKey, ContentType = stored.ContentType, SizeBytes = stored.SizeBytes, IsPrimary = upload.IsPrimary || animal.Media.Count == 0 };
            await animals.AddMediaAsync(entity, ct);
            await SaveAnimalsAsync(ct, "guardar media de animal", animal.Id, entity.Id);
            logger.LogInformation("Media {MediaId} ({Key}) saved for animal {AnimalId}. Thumbnail: {ThumbnailKey}.", entity.Id, stored.ObjectKey, animal.Id, stored.ThumbnailKey ?? "(none)");
            return await ToMediaResponseAsync(entity, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store media for animal {AnimalId} in media storage.", animal.Id);
            throw;
        }
    }

    public async Task<AnimalStatsResponse> GetStatsAsync(Guid animalId, Guid? actorShelterId, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Mascota no encontrada.");
        EnsureShelterAccess(animal, actorShelterId);
        var (postCount, totalViews, totalShares, postIds) = await posts.GetAnimalPostStatsAsync(animalId, ct);
        var totalLikes = postIds.Count == 0 ? 0 : (await likes.CountManyAsync(postIds, ct)).Values.Sum();
        var totalComments = postIds.Count == 0 ? 0 : (await comments.CountManyAsync(postIds, ct)).Values.Sum();
        var followerCount = await follows.CountAsync(animalId, ct);
        return new AnimalStatsResponse(animal.Id, animal.Name, animal.AdoptionStatus, postCount, totalLikes, totalComments, totalViews, totalShares, followerCount);
    }

    /// <summary>
    /// Wraps animals.SaveAsync with diagnostics for DbUpdateConcurrencyException/DbUpdateException —
    /// both surface as an opaque "0 rows affected" message otherwise, with no indication of which
    /// entity/table was actually involved.
    /// </summary>
    private async Task SaveAnimalsAsync(CancellationToken ct, string operation, Guid animalId, Guid? mediaId = null)
    {
        try
        {
            await animals.SaveAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflicts = string.Join("; ", ex.Entries.Select(e => $"{e.Entity.GetType().Name}(id={e.Property("Id").CurrentValue}, state={e.State})"));
            logger.LogError(ex, "Concurrency conflict while trying to {Operation} (animal {AnimalId}, media {MediaId}). Conflicting entries: {Conflicts}.", operation, animalId, mediaId, conflicts);
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while trying to {Operation} (animal {AnimalId}, media {MediaId}). {InnerMessage}", operation, animalId, mediaId, ex.InnerException?.Message ?? ex.Message);
            throw;
        }
    }

    private async Task SaveSheltersAsync(CancellationToken ct, string operation, Guid shelterId)
    {
        try
        {
            await shelters.SaveAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflicts = string.Join("; ", ex.Entries.Select(e => $"{e.Entity.GetType().Name}(state={e.State})"));
            logger.LogError(ex, "Concurrency conflict while trying to {Operation} (shelter {ShelterId}). Conflicting entries: {Conflicts}.", operation, shelterId, conflicts);
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error while trying to {Operation} (shelter {ShelterId}). {InnerMessage}", operation, shelterId, ex.InnerException?.Message ?? ex.Message);
            throw;
        }
    }

    private static ShelterResponse ToResponse(Shelter x) => new(x.Id, x.Name, x.Description, x.Address, x.City, x.Country, x.Phone, x.WhatsApp, x.Email, x.Latitude, x.Longitude, x.Animals.Count);
    private async Task<AnimalMediaResponse> ToMediaResponseAsync(AnimalMedia m, CancellationToken ct) => new(m.Id, await minioService.GetImageUrlAsync(m.ObjectKey, m.ContentType, ct), await minioService.GetThumbnailUrlAsync(m.ThumbnailObjectKey, ct), m.ContentType, m.IsPrimary);
    private async Task<AnimalResponse> ToResponseAsync(Animal x, CancellationToken ct) => new(x.Id, x.ShelterId, x.Shelter?.Name ?? "", x.Name, x.Species, x.Sex, x.Size, x.AgeMonths, x.Breed, x.Description, x.AdoptionStatus, x.Location, (await Task.WhenAll(x.Media.Select(m => ToMediaResponseAsync(m, ct)))).ToArray());
}
