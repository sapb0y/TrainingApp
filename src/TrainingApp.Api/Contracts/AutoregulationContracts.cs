namespace TrainingApp.Api.Contracts;

public record StartWorkoutRequest(int? PreWorkoutReadiness);

public record CompleteWorkoutRequest(int? SessionRpe, int? PostWorkoutFatigue, string? Notes);

public record WorkoutRecommendationsResponse(
    List<ExerciseRecommendationResponse> Exercises,
    string? SessionNote);

public record ExerciseRecommendationResponse(
    Guid ExerciseId,
    string ExerciseName,
    string VolumeAction,
    NextSetResponse? NextSet,
    string Reason);

public record NextSetResponse(decimal Weight, int Reps, int TargetRir);

public record AdaptationLogResponse(
    Guid Id,
    string RuleName,
    string Scope,
    string InputSummary,
    string OutputSummary,
    bool WasApplied,
    DateTimeOffset CreatedAt);
