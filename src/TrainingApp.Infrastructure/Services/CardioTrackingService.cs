using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class CardioTrackingService : ICardioTrackingService
{
    private readonly TrainingAppDbContext _db;

    public CardioTrackingService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<CardioSession> LogCardioAsync(
        Guid userId, DateOnly date, CardioModality modality,
        CardioIntensityZone zone, int durationMinutes, decimal? distanceKm,
        int? avgHr, int? maxHr, DateTimeOffset? startedAt, string? notes, CancellationToken ct = default)
    {
        var stress = InterferenceCalculatorService.CalculateCardioTrimp(modality, zone, durationMinutes);

        var session = new CardioSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = date,
            Modality = modality,
            Zone = zone,
            DurationMinutes = durationMinutes,
            DistanceKm = distanceKm,
            AverageHeartRate = avgHr,
            MaxHeartRate = maxHr,
            CardioTrimp = stress.Trimp,
            Notes = notes,
            StartedAt = startedAt,
            CompletedAt = startedAt?.AddMinutes(durationMinutes)
        };

        _db.CardioSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<CardioSession?> GetCardioSessionAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        return await _db.CardioSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);
    }

    public async Task<List<CardioSession>> GetCardioHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= from && c.Date <= to)
            .OrderBy(c => c.Date)
            .ThenBy(c => c.StartedAt)
            .ToListAsync(ct);
    }

    public async Task DeleteCardioAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var session = await _db.CardioSessions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);

        if (session is not null)
        {
            _db.CardioSessions.Remove(session);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<CardioSession>> GetCardioForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        return await _db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date == date)
            .OrderBy(c => c.StartedAt)
            .ToListAsync(ct);
    }
}
