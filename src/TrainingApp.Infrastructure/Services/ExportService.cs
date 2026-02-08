using System.Text;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly TrainingAppDbContext _db;

    public ExportService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportWorkoutsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var sets = await _db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Include(s => s.Exercise)
            .Where(s => s.Workout!.UserId == userId
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= from
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) <= to)
            .OrderBy(s => s.Workout!.CompletedAt)
            .ThenBy(s => s.SetNumber)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Exercise,Set#,Weight,Reps,RPE,e1RM,Notes");

        foreach (var s in sets)
        {
            var date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime).ToString("yyyy-MM-dd");
            var exercise = EscapeCsv(s.Exercise?.Name ?? "Unknown");
            var weight = s.ActualWeight?.ToString("F1") ?? "";
            var reps = s.ActualReps?.ToString() ?? "";
            var rpe = s.Rpe?.ToString("F1") ?? "";
            var e1rm = s.EstimatedOneRepMax?.ToString("F1") ?? "";
            var notes = EscapeCsv(s.Workout.Notes ?? "");
            sb.AppendLine($"{date},{exercise},{s.SetNumber},{weight},{reps},{rpe},{e1rm},{notes}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportWeightLogsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var logs = await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Date >= from && w.Date <= to)
            .OrderBy(w => w.Date)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,WeightKg,MA7d,MA30d,WeeklyRate");

        foreach (var w in logs)
        {
            sb.AppendLine($"{w.Date:yyyy-MM-dd},{w.WeightKg:F1},{w.MovingAverage7d?.ToString("F1") ?? ""},{w.MovingAverage30d?.ToString("F1") ?? ""},{w.WeeklyRateKg?.ToString("F2") ?? ""}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportCardioSessionsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var sessions = await _db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= from && c.Date <= to)
            .OrderBy(c => c.Date)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Modality,Zone,Duration,Distance,AvgHR,TRIMP");

        foreach (var c in sessions)
        {
            sb.AppendLine($"{c.Date:yyyy-MM-dd},{c.Modality},{c.Zone},{c.DurationMinutes},{c.DistanceKm?.ToString("F1") ?? ""},{c.AverageHeartRate?.ToString() ?? ""},{c.CardioTrimp?.ToString("F1") ?? ""}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
