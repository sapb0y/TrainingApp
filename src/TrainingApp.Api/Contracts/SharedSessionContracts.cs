namespace TrainingApp.Api.Contracts;

public record CreateSharedSessionRequest(Guid PartnershipId, string Date,
    Guid? WorkoutAId, Guid? WorkoutBId, string? Notes);
public record SharedSessionResponse(Guid Id, Guid PartnershipId, string ScheduledDate,
    string Status, Guid? WorkoutAId, Guid? WorkoutBId,
    int? EstimatedDurationMinutes, int? SoloEstimateMinutesA, int? SoloEstimateMinutesB,
    string? Notes, DateTimeOffset CreatedAt,
    List<ScheduleBlockResponse>? Schedule, ScheduleSummaryResponse? Summary,
    List<PartnerAlertResponse>? Alerts);
public record ScheduleBlockResponse(int BlockOrder, SlotActionResponse? UserA,
    SlotActionResponse? UserB, int DurationSeconds);
public record SlotActionResponse(Guid? ExerciseId, string? ExerciseName,
    int? SetNumber, string Type, List<string>? Equipment);
public record ScheduleSummaryResponse(int TotalSeconds, int SoloSecondsA, int SoloSecondsB,
    int TimeSavedSeconds, int EquipmentConflicts,
    decimal UtilizationPercentA, decimal UtilizationPercentB, List<string> Warnings);
public record PartnerAlertResponse(string RuleName, string Severity, string Message);
public record SharedSessionListResponse(List<SharedSessionResponse> Items, int TotalCount);
