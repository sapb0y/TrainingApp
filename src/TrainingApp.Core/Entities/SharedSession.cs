namespace TrainingApp.Core.Entities;

public class SharedSession
{
    public Guid Id { get; set; }
    public Guid PartnershipId { get; set; }
    public Partnership? Partnership { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public SharedSessionStatus Status { get; set; } = SharedSessionStatus.Planned;
    public Guid? WorkoutAId { get; set; }
    public Workout? WorkoutA { get; set; }
    public Guid? WorkoutBId { get; set; }
    public Workout? WorkoutB { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int? SoloEstimateMinutesA { get; set; }
    public int? SoloEstimateMinutesB { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<SharedSessionSlot> Slots { get; set; } = [];
}

public enum SharedSessionStatus { Planned, InProgress, Completed, Cancelled }
