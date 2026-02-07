using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class DeficitPhaseService : IDeficitPhaseService
{
    private readonly TrainingAppDbContext _db;

    public DeficitPhaseService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<DeficitPhase> StartDeficitAsync(Guid userId, decimal startWeightKg, decimal? targetWeightKg,
        decimal weeklyRateKg, DeficitStrategy strategy, int? dietBreakIntervalWeeks, string? notes, CancellationToken ct = default)
    {
        var active = await _db.DeficitPhases
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active, ct);

        if (active is not null)
            throw new InvalidOperationException("An active deficit phase already exists. End or pause it first.");

        var phase = new DeficitPhase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StartWeightKg = startWeightKg,
            TargetWeightKg = targetWeightKg,
            WeeklyRateKg = weeklyRateKg,
            Strategy = strategy,
            Status = DeficitPhaseStatus.Active,
            DietBreakIntervalWeeks = dietBreakIntervalWeeks,
            Notes = notes
        };

        _db.DeficitPhases.Add(phase);
        await _db.SaveChangesAsync(ct);
        return phase;
    }

    public async Task<DeficitPhase?> GetActiveDeficitAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.DeficitPhases
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active, ct);
    }

    public async Task<DeficitPhase> EndDeficitAsync(Guid userId, CancellationToken ct = default)
    {
        var phase = await _db.DeficitPhases
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active, ct)
            ?? throw new InvalidOperationException("No active deficit phase found.");

        phase.Status = DeficitPhaseStatus.Completed;
        phase.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        phase.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return phase;
    }

    public async Task<DeficitPhase> PauseDeficitAsync(Guid userId, CancellationToken ct = default)
    {
        var phase = await _db.DeficitPhases
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active, ct)
            ?? throw new InvalidOperationException("No active deficit phase found.");

        phase.Status = DeficitPhaseStatus.Paused;
        phase.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return phase;
    }

    public async Task<DeficitPhase> ResumeDeficitAsync(Guid userId, CancellationToken ct = default)
    {
        var phase = await _db.DeficitPhases
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Paused, ct)
            ?? throw new InvalidOperationException("No paused deficit phase found.");

        phase.Status = DeficitPhaseStatus.Active;
        phase.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return phase;
    }

    public async Task<List<DeficitPhase>> GetDeficitHistoryAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.DeficitPhases
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync(ct);
    }
}
