namespace TrainingApp.Api.Contracts;

public record ReadinessResponse(
    decimal Score,
    string Category,
    string Recommendation,
    decimal Ctl,
    decimal Atl,
    decimal Tsb,
    string Date,
    RecoveryLogResponse? TodayRecovery,
    List<FatigueAlertResponse> Alerts);

public record FatigueAlertResponse(string RuleName, string Severity, string Message);

public record DailyMetricsResponse(
    Guid Id,
    string Date,
    decimal Trimp,
    decimal Ctl,
    decimal Atl,
    decimal Tsb,
    int TotalSets,
    int TotalReps,
    decimal TotalVolume,
    int WorkoutCount,
    decimal? AverageSessionRpe,
    decimal? ReadinessScore);

public record DailyMetricsListResponse(List<DailyMetricsResponse> Items, int TotalCount);

public record CreateRecoveryLogRequest(
    string Date,
    int? SleepQuality,
    decimal? SleepHours,
    int? StressLevel,
    int? EnergyLevel,
    int? MuscleReadiness,
    int? Mood,
    string? Notes);

public record RecoveryLogResponse(
    Guid Id,
    string Date,
    int? SleepQuality,
    decimal? SleepHours,
    int? StressLevel,
    int? EnergyLevel,
    int? MuscleReadiness,
    int? Mood,
    string? Notes,
    DateTimeOffset CreatedAt);

public record RecoveryLogListResponse(List<RecoveryLogResponse> Items, int TotalCount);

public record RecalculateRequest(string? From);
