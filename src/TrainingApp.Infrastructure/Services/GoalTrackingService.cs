using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class GoalTrackingService : IGoalTrackingService
{
    private readonly TrainingAppDbContext _db;

    public GoalTrackingService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<Goal> CreateGoalAsync(Guid userId, string title, GoalType type, decimal? targetValue,
        string? targetUnit, Guid? exerciseId, decimal? startValue, DateOnly? targetDate, string? notes, CancellationToken ct)
    {
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Type = type,
            TargetValue = targetValue,
            TargetUnit = targetUnit,
            ExerciseId = exerciseId,
            StartValue = startValue,
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TargetDate = targetDate,
            Notes = notes
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task<Goal?> GetGoalAsync(Guid userId, Guid id, CancellationToken ct)
    {
        return await _db.Goals
            .AsNoTracking()
            .Include(g => g.Checkpoints.OrderBy(c => c.Date))
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);
    }

    public async Task<List<Goal>> GetGoalsAsync(Guid userId, GoalStatus? status, CancellationToken ct)
    {
        var query = _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId);

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        return await query
            .Include(g => g.Checkpoints)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Goal> UpdateGoalAsync(Guid userId, Guid id, GoalStatus? status, decimal? targetValue,
        DateOnly? targetDate, string? notes, CancellationToken ct)
    {
        var goal = await _db.Goals
            .Include(g => g.Checkpoints)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct)
            ?? throw new NotFoundException("Goal");

        if (status.HasValue)
        {
            goal.Status = status.Value;
            if (status.Value == GoalStatus.Achieved)
                goal.AchievedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        if (targetValue.HasValue)
            goal.TargetValue = targetValue.Value;

        if (targetDate.HasValue)
            goal.TargetDate = targetDate.Value;

        if (notes is not null)
            goal.Notes = notes;

        await _db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task DeleteGoalAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var goal = await _db.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct)
            ?? throw new NotFoundException("Goal");

        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GoalCheckpoint> AddCheckpointAsync(Guid userId, Guid goalId, DateOnly date, decimal value, string? notes, CancellationToken ct)
    {
        var goal = await _db.Goals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, ct)
            ?? throw new NotFoundException("Goal");

        decimal? percentComplete = null;
        if (goal.TargetValue.HasValue && goal.StartValue.HasValue && goal.TargetValue.Value != goal.StartValue.Value)
        {
            percentComplete = Math.Round(
                (value - goal.StartValue.Value) / (goal.TargetValue.Value - goal.StartValue.Value) * 100m, 1);
            percentComplete = Math.Clamp(percentComplete.Value, 0m, 100m);
        }

        var checkpoint = new GoalCheckpoint
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            Date = date,
            Value = value,
            PercentComplete = percentComplete,
            Notes = notes
        };

        _db.GoalCheckpoints.Add(checkpoint);
        await _db.SaveChangesAsync(ct);
        return checkpoint;
    }

    public async Task<List<GoalCheckpoint>> GetCheckpointsAsync(Guid userId, Guid goalId, CancellationToken ct)
    {
        // Verify ownership
        var goalExists = await _db.Goals.AnyAsync(g => g.Id == goalId && g.UserId == userId, ct);
        if (!goalExists)
            throw new NotFoundException("Goal");

        return await _db.GoalCheckpoints
            .AsNoTracking()
            .Where(c => c.GoalId == goalId)
            .OrderBy(c => c.Date)
            .ToListAsync(ct);
    }
}
