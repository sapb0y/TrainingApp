using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class SharedSessionService : ISharedSessionService
{
    private readonly TrainingAppDbContext _db;

    public SharedSessionService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<SharedSession> CreateSessionAsync(Guid userId, Guid partnershipId,
        DateOnly date, Guid? workoutAId, Guid? workoutBId, string? notes, CancellationToken ct)
    {
        var partnership = await _db.Partnerships
            .FirstOrDefaultAsync(p => p.Id == partnershipId
                && p.Status == PartnershipStatus.Active
                && (p.RequesterId == userId || p.ResponderId == userId), ct)
            ?? throw new InvalidOperationException("Active partnership not found.");

        var session = new SharedSession
        {
            Id = Guid.NewGuid(),
            PartnershipId = partnershipId,
            ScheduledDate = date,
            WorkoutAId = workoutAId,
            WorkoutBId = workoutBId,
            Notes = notes
        };

        _db.SharedSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<SharedSession?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        return await _db.SharedSessions
            .AsNoTracking()
            .Include(s => s.Partnership)
            .Include(s => s.Slots.OrderBy(sl => sl.SlotOrder))
            .FirstOrDefaultAsync(s => s.Id == sessionId
                && (s.Partnership!.RequesterId == userId || s.Partnership.ResponderId == userId), ct);
    }

    public async Task<List<SharedSession>> GetSessionsAsync(Guid userId, Guid partnershipId, CancellationToken ct)
    {
        return await _db.SharedSessions
            .AsNoTracking()
            .Include(s => s.Partnership)
            .Where(s => s.PartnershipId == partnershipId
                && (s.Partnership!.RequesterId == userId || s.Partnership.ResponderId == userId))
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync(ct);
    }

    public async Task<SharedSession> GenerateScheduleAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await _db.SharedSessions
            .Include(s => s.Partnership)
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.Id == sessionId
                && (s.Partnership!.RequesterId == userId || s.Partnership.ResponderId == userId), ct)
            ?? throw new InvalidOperationException("Shared session not found.");

        // Remove existing slots
        _db.SharedSessionSlots.RemoveRange(session.Slots);

        // Load workouts with sets and exercises
        var planA = new List<PartnerSchedulingService.ExercisePlan>();
        var planB = new List<PartnerSchedulingService.ExercisePlan>();

        if (session.WorkoutAId.HasValue)
            planA = await BuildExercisePlan(session.WorkoutAId.Value, session.Partnership!.RequesterId, ct);

        if (session.WorkoutBId.HasValue)
            planB = await BuildExercisePlan(session.WorkoutBId.Value, session.Partnership!.ResponderId!.Value, ct);

        var schedule = PartnerSchedulingService.GenerateSchedule(planA, planB);

        // Persist slots — use _db.Add to ensure EF marks them as Added
        foreach (var block in schedule.Blocks)
        {
            var slot = new SharedSessionSlot
            {
                Id = Guid.NewGuid(),
                SharedSessionId = sessionId,
                SlotOrder = block.BlockOrder,
                UserAExerciseId = block.UserA.ExerciseId,
                UserASetNumber = block.UserA.SetNumber,
                UserAAction = block.UserA.Type.ToString(),
                UserBExerciseId = block.UserB.ExerciseId,
                UserBSetNumber = block.UserB.SetNumber,
                UserBAction = block.UserB.Type.ToString(),
                DurationSeconds = block.DurationSeconds,
                IsParallel = block.UserA.Type == PartnerSchedulingService.ActionType.Work
                             && block.UserB.Type == PartnerSchedulingService.ActionType.Work,
                EquipmentNote = BuildEquipmentNote(block)
            };
            _db.SharedSessionSlots.Add(slot);
        }

        session.EstimatedDurationMinutes = (int)Math.Ceiling(schedule.Summary.TotalSeconds / 60.0);
        session.SoloEstimateMinutesA = (int)Math.Ceiling(schedule.Summary.SoloSecondsA / 60.0);
        session.SoloEstimateMinutesB = (int)Math.Ceiling(schedule.Summary.SoloSecondsB / 60.0);

        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<SharedSession> CancelSessionAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await _db.SharedSessions
            .Include(s => s.Partnership)
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.Id == sessionId
                && (s.Partnership!.RequesterId == userId || s.Partnership.ResponderId == userId), ct)
            ?? throw new InvalidOperationException("Shared session not found.");

        session.Status = SharedSessionStatus.Cancelled;
        _db.SharedSessionSlots.RemoveRange(session.Slots);

        await _db.SaveChangesAsync(ct);
        return session;
    }

    private async Task<List<PartnerSchedulingService.ExercisePlan>> BuildExercisePlan(Guid workoutId, Guid userId, CancellationToken ct)
    {
        var sets = await _db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Exercise)
            .Where(s => s.WorkoutId == workoutId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync(ct);

        var grouped = sets
            .Where(s => s.Exercise is not null)
            .GroupBy(s => s.ExerciseId)
            .Select((g, idx) =>
            {
                var exercise = g.First().Exercise!;
                return new PartnerSchedulingService.ExercisePlan(
                    userId,
                    exercise.Id,
                    exercise.Name,
                    exercise.Equipment,
                    exercise.Category,
                    g.Count(),
                    PartnerSchedulingService.EstimateSetDuration(exercise.Category),
                    90, // default rest
                    idx);
            })
            .ToList();

        return grouped;
    }

    private static string? BuildEquipmentNote(PartnerSchedulingService.ScheduleBlock block)
    {
        var equipA = block.UserA.Equipment;
        var equipB = block.UserB.Equipment;
        if (equipA is null or { Count: 0 } && equipB is null or { Count: 0 })
            return null;

        var parts = new List<string>();
        if (equipA is { Count: > 0 })
            parts.Add($"A: {string.Join(", ", equipA)}");
        if (equipB is { Count: > 0 })
            parts.Add($"B: {string.Join(", ", equipB)}");
        return string.Join(" | ", parts);
    }
}
