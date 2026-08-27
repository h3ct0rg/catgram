using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Shelters;
using KindredPaws.Api.Domain.Social;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Entity<Invitation>().HasKey(x => x.Id);
        builder.Entity<Invitation>().HasIndex(x => x.TokenHash).IsUnique();
        builder.Entity<Invitation>().HasIndex(x => new { x.Email, x.UsedAt });
        builder.Entity<Invitation>().Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Entity<Invitation>().Property(x => x.Role).HasMaxLength(80).IsRequired();
        builder.Entity<Shelter>().Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Entity<Animal>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Entity<Animal>().HasIndex(x => new { x.ShelterId, x.AdoptionStatus });
        builder.Entity<AnimalMedia>().Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Entity<Animal>().HasMany(x => x.Media).WithOne(x => x.Animal).HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Post>().HasIndex(x => new { x.Visibility, x.CreatedAt });
        builder.Entity<Post>().HasMany(x => x.Media).WithOne(x => x.Post).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Story>().HasIndex(x => x.ExpiresAt);
        builder.Entity<Story>().HasMany(x => x.Views).WithOne(x => x.Story).HasForeignKey(x => x.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
