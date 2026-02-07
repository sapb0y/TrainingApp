namespace TrainingApp.Core.Entities;

public class DailyMetrics
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }

    // Training stress
    public decimal Trimp { get; set; }

    // Banister model
    public decimal Ctl { get; set; }
    public decimal Atl { get; set; }
    public decimal Tsb { get; set; }

    // Training stats
    public int TotalSets { get; set; }
    public int TotalReps { get; set; }
    public decimal TotalVolume { get; set; }
    public int WorkoutCount { get; set; }
    public decimal? AverageSessionRpe { get; set; }

    // Readiness
    public decimal? ReadinessScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
