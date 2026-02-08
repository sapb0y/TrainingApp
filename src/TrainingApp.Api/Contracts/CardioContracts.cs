namespace TrainingApp.Api.Contracts;

// Cardio
public record LogCardioRequest(string Date, string Modality, string Zone, int DurationMinutes,
    decimal? DistanceKm, int? AverageHeartRate, int? MaxHeartRate, string? StartedAt, string? Notes);
public record CardioSessionResponse(Guid Id, string Date, string Modality, string Zone,
    int DurationMinutes, decimal? DistanceKm, int? AverageHeartRate, int? MaxHeartRate,
    decimal? CardioTrimp, string? Notes, string? StartedAt, string? CompletedAt, DateTimeOffset CreatedAt);
public record CardioSessionListResponse(List<CardioSessionResponse> Items, int TotalCount);

// Training Day (computed)
public record TrainingDaySummaryResponse(string Date, int StrengthSessions, int CardioSessions,
    decimal StrengthTrimp, decimal CardioTrimp, decimal TotalTrimp,
    decimal InterferenceScore, string InterferenceLevel,
    SequencingResponse Sequencing, List<CardioAlertResponse> Alerts);
public record SequencingResponse(string RecommendedOrder, int SeparationHours, string Rationale);
public record CardioAlertResponse(string RuleName, string Severity, string Message);

// Weekly Summary (computed)
public record WeeklyCardioSummaryResponse(string WeekStart, int TotalSessions, int TotalMinutes,
    decimal TotalDistanceKm, decimal TotalTrimp, Dictionary<string, int> MinutesByZone);
