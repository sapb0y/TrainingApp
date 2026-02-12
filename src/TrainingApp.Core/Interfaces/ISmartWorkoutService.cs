namespace TrainingApp.Core.Interfaces;

public enum WorkoutType
{
    Strength,
    Power,
    Hypertrophy,
    Endurance,
    MetabolicConditioning,
    Recovery,
    Mobility,
    HIIT,
    Deload
}

public record WorkoutOption(
    string Name,
    WorkoutType Type,
    int DurationMinutes,
    string Description,
    List<WorkoutOptionExercise> Exercises
);

public record WorkoutOptionExercise(
    Guid ExerciseId,
    string ExerciseName,
    int Sets,
    int Reps,
    decimal? SuggestedWeight,
    bool IsWarmup = false
);

public interface ISmartWorkoutService
{
    Task<List<WorkoutOption>> GenerateWorkoutOptionsAsync(Guid userId, int durationMinutes = 45, CancellationToken ct = default);
}
