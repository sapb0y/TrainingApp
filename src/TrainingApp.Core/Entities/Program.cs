namespace TrainingApp.Core.Entities;

public class Program
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProgramGoal Goal { get; set; }
    public ProgramTemplate Template { get; set; }
    public ProgramStatus Status { get; set; } = ProgramStatus.Draft;
    public int DurationWeeks { get; set; }
    public DateOnly StartDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProgramPhase> Phases { get; set; } = [];
    public ICollection<Workout> Workouts { get; set; } = [];
}

public enum ProgramGoal
{
    Hypertrophy,
    Strength,
    PowerBuilding,
    GeneralFitness
}

public enum ProgramTemplate
{
    PushPullLegs,
    UpperLower,
    FullBody,
    BroSplit
}

public enum ProgramStatus
{
    Draft,
    Active,
    Completed,
    Archived
}
