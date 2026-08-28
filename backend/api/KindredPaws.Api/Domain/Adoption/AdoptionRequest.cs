namespace KindredPaws.Api.Domain.Adoption;

public enum AdoptionRequestStatus { Pending, InReview, Approved, Rejected, Completed }

public sealed class AdoptionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnimalId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public AdoptionRequestStatus Status { get; set; } = AdoptionRequestStatus.Pending;
    public string AnswersJson { get; set; } = "{}";
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
