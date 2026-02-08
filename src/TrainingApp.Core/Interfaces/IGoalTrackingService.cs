using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IGoalTrackingService
{
    Task<Goal> CreateGoalAsync(Guid userId, string title, GoalType type, decimal? targetValue,
        string? targetUnit, Guid? exerciseId, decimal? startValue, DateOnly? targetDate, string? notes, CancellationToken ct);
    Task<Goal?> GetGoalAsync(Guid userId, Guid id, CancellationToken ct);
    Task<List<Goal>> GetGoalsAsync(Guid userId, GoalStatus? status, CancellationToken ct);
    Task<Goal> UpdateGoalAsync(Guid userId, Guid id, GoalStatus? status, decimal? targetValue,
        DateOnly? targetDate, string? notes, CancellationToken ct);
    Task DeleteGoalAsync(Guid userId, Guid id, CancellationToken ct);
    Task<GoalCheckpoint> AddCheckpointAsync(Guid userId, Guid goalId, DateOnly date, decimal value, string? notes, CancellationToken ct);
    Task<List<GoalCheckpoint>> GetCheckpointsAsync(Guid userId, Guid goalId, CancellationToken ct);
}
