using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;
using KindredPaws.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Social;

public sealed class SocialService(
    SocialRepository repository,
    FollowRepository follows,
    LikeRepository likes,
    CommentRepository comments,
    IMediaStorage storage,
    IThumbnailGenerator thumbnailGenerator,
    UserManager<ApplicationUser> userManager,
    INotificationService notifications,
    IEventPublisher eventPublisher,
    IAuditService audit) : ISocialService
{
    public async Task<PostResponse> CreatePostAsync(CreatePostRequest r, Guid createdByUserId, Guid? actorShelterId, IReadOnlyCollection<MediaUpload> media, CancellationToken ct)
    {
        var effectiveShelterId = actorShelterId ?? r.ShelterId;
        var animal = await repository.GetAnimalAsync(r.AnimalId, effectiveShelterId, ct) ?? throw new KeyNotFoundException("Animal no encontrado en el refugio.");
        var post = new Post { ShelterId = effectiveShelterId, AnimalId = r.AnimalId, CreatedByUserId = createdByUserId, Caption = r.Caption.Trim(), Location = r.Location, Hashtags = r.Hashtags, IsFeatured = r.IsFeatured, IsSuccessStory = r.IsSuccessStory };
        await AddPostMediaAsync(post, media, ct); await repository.AddPostAsync(post, ct); await repository.SaveAsync(ct);
        await NotifyFollowersOfNewPostAsync(animal, post, ct);
        return await ToResponseAsync(post, animal, null, ct);
    }

    public async Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest r, Guid? actorShelterId, CancellationToken ct)
    {
        var post = await repository.GetPostAsync(id, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        EnsureShelterAccess(post.ShelterId, actorShelterId);
        post.Caption = r.Caption.Trim(); post.Location = r.Location; post.Hashtags = r.Hashtags; post.IsFeatured = r.IsFeatured; post.UpdatedAt = DateTimeOffset.UtcNow; await repository.SaveAsync(ct);
        var animal = (await repository.GetAnimalsByIdsAsync([post.AnimalId], ct)).GetValueOrDefault(post.AnimalId);
        return await ToResponseAsync(post, animal, null, ct);
    }

    public async Task HidePostAsync(Guid id, Guid actorUserId, Guid? actorShelterId, CancellationToken ct)
    {
        var post = await repository.GetPostAsync(id, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        EnsureShelterAccess(post.ShelterId, actorShelterId);
        post.Visibility = ContentVisibility.Hidden; await repository.SaveAsync(ct);
        await audit.RecordAsync(actorUserId, AuditAction.PostHidden, "Post", id, null, ct);
    }

    public async Task<PostResponse> GetPostAsync(Guid id, Guid? currentUserId, CancellationToken ct)
    {
        var post = await repository.GetPostAsync(id, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        if (post.Visibility != ContentVisibility.Published) throw new KeyNotFoundException("Publicación no encontrada.");
        var animal = (await repository.GetAnimalsByIdsAsync([post.AnimalId], ct)).GetValueOrDefault(post.AnimalId);
        return await ToResponseAsync(post, animal, currentUserId, ct);
    }

    public async Task<IReadOnlyCollection<PostResponse>> GetFeedAsync(DateTimeOffset? before, int skip, int pageSize, string sort, bool successStoriesOnly, Guid? currentUserId, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        skip = Math.Max(skip, 0);
        var posts = await repository.ListFeedAsync(before, skip, pageSize, sort == "popular", successStoriesOnly, ct);
        if (posts.Count == 0) return [];

        var postIds = posts.Select(x => x.Id).ToArray();
        var animalIds = posts.Select(x => x.AnimalId).Distinct().ToArray();
        var animalsById = await repository.GetAnimalsByIdsAsync(animalIds, ct);
        var likeCounts = await likes.CountManyAsync(postIds, ct);
        var commentCounts = await comments.CountManyAsync(postIds, ct);
        var likedPostIds = currentUserId.HasValue ? await likes.ListLikedPostIdsAsync(currentUserId.Value, postIds, ct) : [];

        return await Task.WhenAll(posts.Select(post => ToResponseAsync(
            post,
            animalsById.GetValueOrDefault(post.AnimalId),
            null,
            ct,
            likeCounts.GetValueOrDefault(post.Id),
            commentCounts.GetValueOrDefault(post.Id),
            likedPostIds.Contains(post.Id))));
    }

    public async Task<StoryResponse> CreateStoryAsync(CreateStoryRequest r, Guid? actorShelterId, MediaUpload media, CancellationToken ct)
    {
        var effectiveShelterId = actorShelterId ?? r.ShelterId;
        var valid = await repository.AnimalExistsAsync(r.AnimalId, effectiveShelterId, ct); if (!valid) throw new KeyNotFoundException("Animal no encontrado en el refugio.");
        ValidateMedia(media);
        var key = $"stories/{Guid.NewGuid():N}-{Path.GetFileName(media.FileName)}"; await storage.PutAsync(key, media.Content, media.Length, media.ContentType, ct);
        var story = new Story { ShelterId = effectiveShelterId, AnimalId = r.AnimalId, Caption = r.Caption.Trim(), ObjectKey = key, ContentType = media.ContentType }; await repository.AddStoryAsync(story, ct); await repository.SaveAsync(ct);
        var animal = (await repository.GetAnimalsByIdsAsync([story.AnimalId], ct)).GetValueOrDefault(story.AnimalId);
        return await ToStoryResponseAsync(story, animal, ct);
    }

    public Task RegisterPostViewAsync(Guid id, CancellationToken ct) => repository.IncrementViewCountAsync(id, ct);

    public Task RegisterPostShareAsync(Guid id, CancellationToken ct) => repository.IncrementShareCountAsync(id, ct);

    public async Task<IReadOnlyCollection<StoryResponse>> GetStoriesAsync(CancellationToken ct)
    {
        var stories = await repository.ListStoriesAsync(ct);
        if (stories.Count == 0) return [];
        var animalsById = await repository.GetAnimalsByIdsAsync(stories.Select(x => x.AnimalId).Distinct().ToArray(), ct);
        return await Task.WhenAll(stories.Select(x => ToStoryResponseAsync(x, animalsById.GetValueOrDefault(x.AnimalId), ct)));
    }

    public async Task RegisterStoryViewAsync(Guid id, string? anonymousKey, Guid? userId, CancellationToken ct)
    {
        var story = await repository.GetStoryAsync(id, ct) ?? throw new KeyNotFoundException("Historia no encontrada o expirada.");
        if (userId is null && string.IsNullOrWhiteSpace(anonymousKey)) anonymousKey = Guid.NewGuid().ToString("N");
        await repository.AddViewAsync(new StoryView { StoryId = story.Id, UserId = userId, AnonymousKey = anonymousKey }, ct); await repository.SaveAsync(ct);
    }

    private async Task NotifyFollowersOfNewPostAsync(Animal animal, Post post, CancellationToken ct)
    {
        var followerIds = await follows.ListFollowerIdsAsync(animal.Id, ct);
        if (followerIds.Count == 0) return;
        var followers = await userManager.Users.Where(u => followerIds.Contains(u.Id)).ToListAsync(ct);
        foreach (var follower in followers)
        {
            await notifications.CreateAsync(follower.Id, NotificationType.NewPost, "Nueva publicación", $"{animal.Name} tiene una publicación nueva.", $"/p/{post.Id}", post.Id, ct);
            await eventPublisher.PublishAsync(new PostCreatedEvent(post.Id, animal.Id, animal.Name, post.ShelterId, follower.Id, follower.Email ?? string.Empty, follower.FullName), ct);
        }
    }

    private async Task AddPostMediaAsync(Post post, IReadOnlyCollection<MediaUpload> media, CancellationToken ct)
    {
        foreach (var upload in media.Take(10))
        {
            ValidateMedia(upload);
            using var buffer = new MemoryStream();
            await upload.Content.CopyToAsync(buffer, ct);
            var key = $"posts/{post.Id:N}/{Guid.NewGuid():N}-{Path.GetFileName(upload.FileName)}";
            buffer.Position = 0;
            await storage.PutAsync(key, buffer, upload.Length, upload.ContentType, ct);
            var thumbnailKey = await TryGenerateThumbnailAsync(buffer, upload.ContentType, $"posts/{post.Id:N}", ct);
            post.Media.Add(new PostMedia { ObjectKey = key, ThumbnailObjectKey = thumbnailKey, ContentType = upload.ContentType, IsPrimary = post.Media.Count == 0 });
        }
    }

    private async Task<string?> TryGenerateThumbnailAsync(MemoryStream buffer, string contentType, string keyPrefix, CancellationToken ct)
    {
        if (!thumbnailGenerator.CanGenerate(contentType)) return null;
        buffer.Position = 0;
        var thumbnail = await thumbnailGenerator.GenerateAsync(buffer, ct);
        if (thumbnail is null) return null;
        var thumbnailKey = $"{keyPrefix}/{Guid.NewGuid():N}-thumb.webp";
        await storage.PutAsync(thumbnailKey, thumbnail.Value.Content, thumbnail.Value.Length, thumbnail.Value.ContentType, ct);
        await thumbnail.Value.Content.DisposeAsync();
        return thumbnailKey;
    }

    /// <summary>actorShelterId is null for SuperAdmin (unrestricted); Administradores are confined to their own shelter's content.</summary>
    private static void EnsureShelterAccess(Guid contentShelterId, Guid? actorShelterId)
    {
        if (actorShelterId.HasValue && contentShelterId != actorShelterId.Value)
            throw new UnauthorizedAccessException("No puedes gestionar contenido de otro refugio.");
    }

    private static void ValidateMedia(MediaUpload m) { if (m.Length <= 0 || m.Length > 50 * 1024 * 1024 || !new[] { "image/jpeg", "image/png", "image/webp", "video/mp4" }.Contains(m.ContentType)) throw new ArgumentException("Archivo no permitido o excede 50 MB."); }
    private async Task<AnimalMediaResponse> ToMediaResponseAsync(PostMedia m, CancellationToken ct) => new(m.Id, await storage.GetUrlAsync(m.ObjectKey, ct), m.ThumbnailObjectKey is null ? null : await storage.GetUrlAsync(m.ThumbnailObjectKey, ct), m.ContentType, m.IsPrimary);

    private async Task<PostResponse> ToResponseAsync(Post x, Animal? animal, Guid? currentUserId, CancellationToken ct, int? likeCount = null, int? commentCount = null, bool? likedByCurrentUser = null)
    {
        likeCount ??= await likes.CountAsync(x.Id, ct);
        commentCount ??= await comments.CountAsync(x.Id, ct);
        likedByCurrentUser ??= currentUserId.HasValue && await likes.ExistsAsync(x.Id, currentUserId.Value, ct);
        return new(
            x.Id, x.ShelterId, animal?.Shelter?.Name ?? "", x.AnimalId, animal?.Name ?? "", animal?.AdoptionStatus.ToString() ?? "",
            x.Caption, x.Location, x.Hashtags, x.IsFeatured, x.IsSuccessStory, x.CreatedAt,
            likeCount.Value, commentCount.Value, likedByCurrentUser.Value,
            (await Task.WhenAll(x.Media.Select(m => ToMediaResponseAsync(m, ct)))).ToArray());
    }

    private async Task<StoryResponse> ToStoryResponseAsync(Story x, Animal? animal, CancellationToken ct) =>
        new(x.Id, x.ShelterId, x.AnimalId, animal?.Name ?? "", x.Caption, await storage.GetUrlAsync(x.ObjectKey, ct), x.ContentType, x.CreatedAt, x.ExpiresAt, x.Views.Count);
}
