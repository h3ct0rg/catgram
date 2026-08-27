namespace KindredPaws.Api.Domain.Social;

public enum ContentVisibility { Published, Hidden, Deleted }

public sealed class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShelterId { get; set; }
    public Guid AnimalId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Hashtags { get; set; }
    public ContentVisibility Visibility { get; set; } = ContentVisibility.Published;
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public int ShareCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<PostMedia> Media { get; set; } = [];
}

public sealed class PostMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string? ThumbnailObjectKey { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public sealed class Story
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShelterId { get; set; }
    public Guid AnimalId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
    public ICollection<StoryView> Views { get; set; } = [];
}

public sealed class StoryView
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }
    public Guid? UserId { get; set; }
    public string? AnonymousKey { get; set; }
    public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;
}
