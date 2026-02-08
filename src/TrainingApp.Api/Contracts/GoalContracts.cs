namespace TrainingApp.Api.Contracts;

public record CreateGoalRequest(string Title, string Type, decimal? TargetValue, string? TargetUnit,
    Guid? ExerciseId, decimal? StartValue, string? TargetDate, string? Notes);
public record UpdateGoalRequest(string? Status, decimal? TargetValue, string? TargetDate, string? Notes);
public record AddCheckpointRequest(string Date, decimal Value, string? Notes);
public record GoalResponse(Guid Id, string Title, string Type, string Status, decimal? TargetValue,
    string? TargetUnit, Guid? ExerciseId, decimal? StartValue, decimal? CurrentValue,
    decimal? PercentComplete, string CreatedDate, string? TargetDate, string? AchievedDate,
    string? Notes, int CheckpointCount, DateTimeOffset CreatedAt);
public record GoalListResponse(List<GoalResponse> Items, int TotalCount);
public record GoalCheckpointResponse(Guid Id, string Date, decimal Value, decimal? PercentComplete, string? Notes);
public record GoalDetailResponse(GoalResponse Goal, List<GoalCheckpointResponse> Checkpoints);
