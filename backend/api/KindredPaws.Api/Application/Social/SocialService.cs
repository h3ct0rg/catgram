using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;

namespace KindredPaws.Api.Application.Social;

public sealed class SocialService(SocialRepository repository, IMediaStorage storage) : ISocialService
{
    public async Task<PostResponse> CreatePostAsync(CreatePostRequest r, IReadOnlyCollection<MediaUpload> media, CancellationToken ct)
    {
        var animal = await repository.GetAnimalAsync(r.AnimalId, r.ShelterId, ct) ?? throw new KeyNotFoundException("Animal no encontrado en el refugio.");
        var post = new Post { ShelterId = r.ShelterId, AnimalId = r.AnimalId, Caption = r.Caption.Trim(), Location = r.Location, Hashtags = r.Hashtags, IsFeatured = r.IsFeatured };
        await AddPostMediaAsync(post, media, ct); await repository.AddPostAsync(post, ct); await repository.SaveAsync(ct); return await ToResponseAsync(post, ct);
    }

    public async Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest r, CancellationToken ct)
    {
        var post = await repository.GetPostAsync(id, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        post.Caption = r.Caption.Trim(); post.Location = r.Location; post.Hashtags = r.Hashtags; post.IsFeatured = r.IsFeatured; post.UpdatedAt = DateTimeOffset.UtcNow; await repository.SaveAsync(ct); return await ToResponseAsync(post, ct);
    }

    public async Task HidePostAsync(Guid id, CancellationToken ct)
    {
        var post = await repository.GetPostAsync(id, ct) ?? throw new KeyNotFoundException("Publicación no encontrada."); post.Visibility = ContentVisibility.Hidden; await repository.SaveAsync(ct);
    }

    public async Task<IReadOnlyCollection<PostResponse>> GetFeedAsync(DateTimeOffset? before, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var posts = await repository.ListFeedAsync(before, pageSize, ct);
        return (await Task.WhenAll(posts.Select(x => ToResponseAsync(x, ct)))).ToArray();
    }

    public async Task<StoryResponse> CreateStoryAsync(CreateStoryRequest r, MediaUpload media, CancellationToken ct)
    {
        var valid = await repository.AnimalExistsAsync(r.AnimalId, r.ShelterId, ct); if (!valid) throw new KeyNotFoundException("Animal no encontrado en el refugio.");
        ValidateMedia(media);
        var key = $"stories/{Guid.NewGuid():N}-{Path.GetFileName(media.FileName)}"; await storage.PutAsync(key, media.Content, media.Length, media.ContentType, ct);
        var story = new Story { ShelterId = r.ShelterId, AnimalId = r.AnimalId, Caption = r.Caption.Trim(), ObjectKey = key, ContentType = media.ContentType }; await repository.AddStoryAsync(story, ct); await repository.SaveAsync(ct); return await ToStoryResponseAsync(story, ct);
    }

    public async Task<IReadOnlyCollection<StoryResponse>> GetStoriesAsync(CancellationToken ct)
    {
        var stories = await repository.ListStoriesAsync(ct);
        return (await Task.WhenAll(stories.Select(x => ToStoryResponseAsync(x, ct)))).ToArray();
    }

    public async Task RegisterStoryViewAsync(Guid id, string? anonymousKey, Guid? userId, CancellationToken ct)
    {
        var story = await repository.GetStoryAsync(id, ct) ?? throw new KeyNotFoundException("Historia no encontrada o expirada.");
        if (userId is null && string.IsNullOrWhiteSpace(anonymousKey)) anonymousKey = Guid.NewGuid().ToString("N");
        await repository.AddViewAsync(new StoryView { StoryId = story.Id, UserId = userId, AnonymousKey = anonymousKey }, ct); await repository.SaveAsync(ct);
    }

    private async Task AddPostMediaAsync(Post post, IReadOnlyCollection<MediaUpload> media, CancellationToken ct)
    {
        foreach (var upload in media.Take(10)) { ValidateMedia(upload); var key = $"posts/{post.Id:N}/{Guid.NewGuid():N}-{Path.GetFileName(upload.FileName)}"; await storage.PutAsync(key, upload.Content, upload.Length, upload.ContentType, ct); post.Media.Add(new PostMedia { ObjectKey = key, ContentType = upload.ContentType, IsPrimary = post.Media.Count == 0 }); }
    }
    private static void ValidateMedia(MediaUpload m) { if (m.Length <= 0 || m.Length > 50 * 1024 * 1024 || !new[] { "image/jpeg", "image/png", "image/webp", "video/mp4" }.Contains(m.ContentType)) throw new ArgumentException("Archivo no permitido o excede 50 MB."); }
    private async Task<PostResponse> ToResponseAsync(Post x, CancellationToken ct) => new(x.Id, x.ShelterId, x.AnimalId, x.Caption, x.Location, x.Hashtags, x.IsFeatured, x.CreatedAt, (await Task.WhenAll(x.Media.Select(async m => new AnimalMediaResponse(m.Id, await storage.GetUrlAsync(m.ObjectKey, ct), m.ContentType, m.IsPrimary)))).ToArray());
    private async Task<StoryResponse> ToStoryResponseAsync(Story x, CancellationToken ct) => new(x.Id, x.ShelterId, x.AnimalId, x.Caption, await storage.GetUrlAsync(x.ObjectKey, ct), x.ContentType, x.CreatedAt, x.ExpiresAt, x.Views.Count);
}
