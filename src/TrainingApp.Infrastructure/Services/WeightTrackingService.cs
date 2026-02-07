using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class WeightTrackingService : IWeightTrackingService
{
    private readonly TrainingAppDbContext _db;

    public WeightTrackingService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<WeightLog> LogWeightAsync(Guid userId, DateOnly date, decimal weightKg, string? notes, CancellationToken ct = default)
    {
        var existing = await _db.WeightLogs
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Date == date, ct);

        if (existing is not null)
        {
            existing.WeightKg = weightKg;
            existing.Notes = notes;
        }
        else
        {
            existing = new WeightLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                WeightKg = weightKg,
                Notes = notes
            };
            _db.WeightLogs.Add(existing);
        }

        // Calculate rolling averages from recent entries
        var recentLogs = await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Date <= date)
            .OrderByDescending(w => w.Date)
            .Take(30)
            .ToListAsync(ct);

        // Include the current entry in calculation (it may not be saved yet)
        var allEntries = recentLogs.Where(w => w.Date != date).ToList();
        allEntries.Insert(0, existing);

        // 7-day EMA
        if (allEntries.Count >= 2)
        {
            var ema7 = allEntries.Last().WeightKg;
            foreach (var entry in allEntries.AsEnumerable().Reverse().Skip(1))
            {
                ema7 = MetabolicCalculatorService.CalculateEma(ema7, entry.WeightKg, 7);
            }
            existing.MovingAverage7d = ema7;
        }
        else
        {
            existing.MovingAverage7d = weightKg;
        }

        // 30-day EMA
        if (allEntries.Count >= 2)
        {
            var ema30 = allEntries.Last().WeightKg;
            foreach (var entry in allEntries.AsEnumerable().Reverse().Skip(1))
            {
                ema30 = MetabolicCalculatorService.CalculateEma(ema30, entry.WeightKg, 30);
            }
            existing.MovingAverage30d = ema30;
        }
        else
        {
            existing.MovingAverage30d = weightKg;
        }

        // Weekly rate from 7d averages (this week vs ~7 days ago)
        var olderEntry = allEntries.Count >= 7 ? allEntries[6] : allEntries.LastOrDefault();
        if (olderEntry is not null && olderEntry.Date != existing.Date)
        {
            existing.WeeklyRateKg = MetabolicCalculatorService.CalculateWeeklyRate(
                olderEntry.MovingAverage7d ?? olderEntry.WeightKg,
                existing.MovingAverage7d ?? existing.WeightKg);
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<List<WeightLog>> GetWeightHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Date >= from && w.Date <= to)
            .OrderBy(w => w.Date)
            .ToListAsync(ct);
    }

    public async Task<WeightLog?> GetLatestWeightAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync(ct);
    }
}
