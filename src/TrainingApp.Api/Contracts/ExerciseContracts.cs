namespace TrainingApp.Api.Contracts;

public record ExerciseResponse(
    Guid Id,
    int? WgerId,
    string Name,
    string? Description,
    string Category,
    List<string> PrimaryMuscles,
    List<string> SecondaryMuscles,
    List<string> Equipment,
    bool IsCustom,
    DateTimeOffset? CachedAt);

public record ExerciseListResponse(
    List<ExerciseResponse> Items,
    int TotalCount);

public record ExerciseSyncResponse(
    int SyncedCount,
    DateTimeOffset SyncedAt);
