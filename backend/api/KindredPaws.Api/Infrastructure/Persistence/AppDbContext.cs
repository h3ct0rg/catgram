using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Shelters;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Domain.Follows;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Domain.Moderation;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Adoption;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalMedia> AnimalMedia => Set<AnimalMedia>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Entity<ApplicationUser>().HasIndex(x => x.ShelterId);
        builder.Entity<ApplicationUser>().HasOne<Shelter>().WithMany().HasForeignKey(x => x.ShelterId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Invitation>().HasKey(x => x.Id);
        builder.Entity<Invitation>().HasOne<Shelter>().WithMany().HasForeignKey(x => x.ShelterId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Invitation>().HasIndex(x => x.TokenHash).IsUnique();
        builder.Entity<Invitation>().HasIndex(x => new { x.Email, x.UsedAt });
        builder.Entity<Invitation>().Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Entity<Invitation>().Property(x => x.Role).HasMaxLength(80).IsRequired();
        builder.Entity<Shelter>().Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Entity<Shelter>().HasIndex(x => x.Name);
        builder.Entity<Animal>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Entity<Animal>().HasIndex(x => new { x.ShelterId, x.AdoptionStatus });
        builder.Entity<Animal>().HasIndex(x => x.Name);
        builder.Entity<Animal>().HasIndex(x => x.Species);
        builder.Entity<Animal>().HasIndex(x => x.Sex);
        builder.Entity<Animal>().HasIndex(x => x.Size);
        builder.Entity<AnimalMedia>().Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Entity<Animal>().HasMany(x => x.Media).WithOne(x => x.Animal).HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Post>().HasIndex(x => new { x.Visibility, x.CreatedAt });
        builder.Entity<Post>().HasIndex(x => x.IsSuccessStory);
        builder.Entity<Post>().HasMany(x => x.Media).WithOne(x => x.Post).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Story>().HasIndex(x => x.ExpiresAt);
        builder.Entity<Story>().HasMany(x => x.Views).WithOne(x => x.Story).HasForeignKey(x => x.StoryId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Like>().HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
        builder.Entity<Like>().HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>().Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Entity<Comment>().HasIndex(x => new { x.PostId, x.CreatedAt });
        builder.Entity<Comment>().HasIndex(x => x.ParentCommentId);
        builder.Entity<Comment>().HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Comment>().HasOne(x => x.ParentComment).WithMany().HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Follow>().HasIndex(x => new { x.UserId, x.AnimalId }).IsUnique();

        builder.Entity<Notification>().Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Entity<Notification>().Property(x => x.Body).HasMaxLength(500).IsRequired();
        builder.Entity<Notification>().HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt });
        builder.Entity<NotificationPreference>().HasIndex(x => new { x.UserId, x.Type }).IsUnique();

        builder.Entity<Report>().Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Entity<Report>().HasIndex(x => new { x.TargetType, x.TargetId });
        builder.Entity<Report>().HasIndex(x => x.Status);

        builder.Entity<AuditLog>().Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Entity<AuditLog>().Property(x => x.Details).HasMaxLength(1000);
        builder.Entity<AuditLog>().HasIndex(x => x.CreatedAt);
        builder.Entity<AuditLog>().HasIndex(x => new { x.EntityType, x.EntityId });

        builder.Entity<AdoptionRequest>().HasIndex(x => new { x.AnimalId, x.Status });
        builder.Entity<AdoptionRequest>().HasIndex(x => x.ApplicantUserId);
    }
}
