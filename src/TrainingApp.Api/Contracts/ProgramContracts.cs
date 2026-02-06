namespace TrainingApp.Api.Contracts;

public record GenerateProgramRequest(
    string Name,
    string Goal,
    string Template,
    int DurationWeeks,
    string StartDate);

public record UpdateProgramRequest(
    string? Name,
    string? Description,
    string? Status);

public record ProgramResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string Goal,
    string Template,
    string Status,
    int DurationWeeks,
    string StartDate,
    DateTimeOffset CreatedAt,
    List<ProgramPhaseResponse> Phases);

public record ProgramSummaryResponse(
    Guid Id,
    string Name,
    string Goal,
    string Template,
    string Status,
    int DurationWeeks,
    DateTimeOffset CreatedAt);

public record ProgramListResponse(
    List<ProgramSummaryResponse> Items,
    int TotalCount);

public record ProgramPhaseResponse(
    Guid Id,
    string Name,
    string Type,
    int OrderIndex,
    int DurationWeeks,
    decimal VolumeMultiplier,
    decimal IntensityMultiplier,
    int TargetRir,
    List<MuscleVolumeTargetResponse> VolumeTargets,
    List<ProgramWorkoutResponse> Workouts);

public record ProgramWorkoutResponse(
    Guid Id,
    string Name,
    string Focus,
    int DayNumber,
    int OrderIndex,
    List<ProgramExerciseResponse> Exercises);

public record ProgramExerciseResponse(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    int OrderIndex,
    int TargetSets,
    int TargetRepsMin,
    int TargetRepsMax,
    decimal? IntensityPercentage,
    decimal? TargetRpe,
    int RestSeconds,
    string? Notes);

public record MuscleVolumeTargetResponse(
    Guid Id,
    string MuscleGroup,
    int MinSetsPerWeek,
    int TargetSetsPerWeek,
    int MaxSetsPerWeek);
