namespace TrainingApp.Api.Contracts;

// Weight
public record LogWeightRequest(string Date, decimal WeightKg, string? Notes);
public record WeightLogResponse(Guid Id, string Date, decimal WeightKg, decimal? MovingAverage7d, decimal? MovingAverage30d, decimal? WeeklyRateKg, string? Notes, DateTimeOffset CreatedAt);
public record WeightLogListResponse(List<WeightLogResponse> Items, int TotalCount);

// Deficit
public record StartDeficitRequest(decimal StartWeightKg, decimal? TargetWeightKg, decimal WeeklyRateKg, string Strategy, int? DietBreakIntervalWeeks, string? Notes);
public record DeficitPhaseResponse(Guid Id, string StartDate, string? EndDate, decimal StartWeightKg, decimal? TargetWeightKg, decimal WeeklyRateKg, string Strategy, string Status, int? DietBreakIntervalWeeks, string? LastDietBreakDate, decimal? CurrentAdaptationPercent, string? Notes, DateTimeOffset CreatedAt);
public record DeficitPhaseListResponse(List<DeficitPhaseResponse> Items, int TotalCount);

// NEAT
public record LogNeatRequest(string Date, int StepCount, string? Notes);
public record NeatLogResponse(Guid Id, string Date, int StepCount, decimal? EstimatedNeatKcal, decimal? CompensationPercent, string? Notes, DateTimeOffset CreatedAt);
public record NeatLogListResponse(List<NeatLogResponse> Items, int TotalCount);

// Metabolism
public record MetabolismSummaryResponse(
    decimal? BmrKcal, decimal? TdeeKcal, decimal? AdjustedTdeeKcal,
    decimal? AdaptationKcal, decimal? AdaptationPercent,
    decimal? NeatCompensationKcal, decimal? IntakeTargetKcal, decimal? DeficitKcal,
    decimal? CurrentWeightKg, decimal? WeeklyRateKg,
    List<WeightProjectionResponse> Projections, List<FatLossAlertResponse> Alerts);
public record WeightProjectionResponse(string Date, decimal ProjectedWeightKg);
public record FatLossAlertResponse(string RuleName, string Severity, string Message);
