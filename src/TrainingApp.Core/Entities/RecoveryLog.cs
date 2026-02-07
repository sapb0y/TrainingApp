namespace TrainingApp.Core.Entities;

public class RecoveryLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }

    public int? SleepQuality { get; set; }
    public decimal? SleepHours { get; set; }
    public int? StressLevel { get; set; }
    public int? EnergyLevel { get; set; }
    public int? MuscleReadiness { get; set; }
    public int? Mood { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
