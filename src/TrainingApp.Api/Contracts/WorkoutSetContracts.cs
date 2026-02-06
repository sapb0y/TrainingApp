namespace TrainingApp.Api.Contracts;

public record WorkoutSetResponse(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    int SetNumber,
    int? TargetReps,
    decimal? TargetWeight,
    int? ActualReps,
    decimal? ActualWeight,
    decimal? Rpe,
    int? Rir,
    int? TargetRir,
    int? RirDrift,
    bool IsWarmup,
    bool WasAutoAdjusted,
    string? AdjustmentReason,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? PerformedAt,
    decimal? EstimatedOneRepMax);

public record CreateWorkoutSetRequest(
    Guid ExerciseId,
    int SetNumber,
    int? TargetReps,
    decimal? TargetWeight,
    int? TargetRir,
    bool IsWarmup = false);

public record UpdateWorkoutSetRequest(
    int? TargetReps,
    decimal? TargetWeight,
    int? ActualReps,
    decimal? ActualWeight,
    decimal? Rpe,
    int? Rir,
    int? TargetRir,
    bool? IsWarmup,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? PerformedAt);
