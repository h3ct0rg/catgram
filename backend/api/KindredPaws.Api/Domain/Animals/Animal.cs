namespace KindredPaws.Api.Domain.Animals;

public enum AdoptionStatus { Available, InProcess, Adopted, Unavailable, Deceased }
public enum AnimalSpecies { Dog, Cat, Other }
public enum AnimalSex { Female, Male, Unknown }
public enum AnimalSize { Small, Medium, Large }

public sealed class Animal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShelterId { get; set; }
    public KindredPaws.Api.Domain.Shelters.Shelter? Shelter { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public AnimalSpecies Species { get; set; }
    public AnimalSex Sex { get; set; }
    public AnimalSize Size { get; set; }
    public int? AgeMonths { get; set; }
    public string Description { get; set; } = string.Empty;
    public AdoptionStatus AdoptionStatus { get; set; } = AdoptionStatus.Available;
    public string? Location { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AnimalMedia> Media { get; set; } = [];
}

public sealed class AnimalMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnimalId { get; set; }
    public Animal? Animal { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string? ThumbnailObjectKey { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
