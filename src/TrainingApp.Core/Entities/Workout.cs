namespace TrainingApp.Core.Entities;

public class Workout
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public WorkoutStatus Status { get; set; } = WorkoutStatus.Planned;
    public int? SessionRpe { get; set; }
    public Guid? ProgramId { get; set; }
    public Program? Program { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<WorkoutSet> Sets { get; set; } = [];
}

public enum WorkoutStatus
{
    Planned,
    InProgress,
    Completed,
    Skipped
}
