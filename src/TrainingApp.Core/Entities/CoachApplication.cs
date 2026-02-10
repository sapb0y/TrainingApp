namespace TrainingApp.Core.Entities;

public class CoachApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Credentials { get; set; }
    public int CurrentClientCount { get; set; }
    public required string BusinessGoal { get; set; }
    public string? AdditionalInfo { get; set; }
    public CoachApplicationStatus Status { get; set; } = CoachApplicationStatus.Pending;
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum CoachApplicationStatus { Pending, Approved, Rejected }
