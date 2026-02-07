namespace TrainingApp.Core.Entities;

public class DeficitPhase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal StartWeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public decimal WeeklyRateKg { get; set; }
    public DeficitStrategy Strategy { get; set; }
    public DeficitPhaseStatus Status { get; set; } = DeficitPhaseStatus.Active;
    public int? DietBreakIntervalWeeks { get; set; }
    public DateOnly? LastDietBreakDate { get; set; }
    public decimal? CurrentAdaptationPercent { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum DeficitStrategy { Conservative, Moderate, Aggressive }
public enum DeficitPhaseStatus { Active, Completed, Paused }
