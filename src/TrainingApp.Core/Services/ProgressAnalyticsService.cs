namespace TrainingApp.Core.Services;

public static class ProgressAnalyticsService
{
    public static string DetectTrend(IEnumerable<(DateOnly Date, decimal Value)> points, int windowDays = 14)
    {
        var list = points.OrderBy(p => p.Date).ToList();
        if (list.Count < 3)
            return "Insufficient Data";

        var cutoff = list.Last().Date.AddDays(-windowDays);
        var window = list.Where(p => p.Date >= cutoff).ToList();
        if (window.Count < 3)
            return "Insufficient Data";

        // Simple linear regression: slope of value over day-index
        var n = window.Count;
        var baseDay = window[0].Date.DayNumber;
        var sumX = 0m;
        var sumY = 0m;
        var sumXy = 0m;
        var sumX2 = 0m;

        foreach (var (date, value) in window)
        {
            var x = (decimal)(date.DayNumber - baseDay);
            sumX += x;
            sumY += value;
            sumXy += x * value;
            sumX2 += x * x;
        }

        var denominator = n * sumX2 - sumX * sumX;
        if (denominator == 0)
            return "Stable";

        var slope = (n * sumXy - sumX * sumY) / denominator;

        // Normalize slope relative to mean value
        var mean = sumY / n;
        if (mean == 0)
            return "Stable";

        var normalizedSlope = slope / mean * 100m; // percent per day

        return normalizedSlope switch
        {
            > 0.2m => "Increasing",
            < -0.2m => "Decreasing",
            _ => "Stable"
        };
    }

    public static List<(string ExerciseName, decimal BestE1rm, DateOnly Date, bool IsRecent)> FindPersonalRecords(
        IEnumerable<(DateOnly Date, decimal E1rm, string ExerciseName)> history)
    {
        var recentCutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        var grouped = history.GroupBy(h => h.ExerciseName, StringComparer.OrdinalIgnoreCase);
        var records = new List<(string ExerciseName, decimal BestE1rm, DateOnly Date, bool IsRecent)>();

        foreach (var group in grouped)
        {
            var best = group.OrderByDescending(h => h.E1rm).ThenByDescending(h => h.Date).First();
            records.Add((best.ExerciseName, best.E1rm, best.Date, best.Date >= recentCutoff));
        }

        return records.OrderByDescending(r => r.BestE1rm).ToList();
    }

    public static decimal CalculateConsistency(IEnumerable<DateOnly> activeDates, DateOnly from, DateOnly to)
    {
        var totalDays = to.DayNumber - from.DayNumber + 1;
        if (totalDays <= 0)
            return 0m;

        var uniqueDays = activeDates.Where(d => d >= from && d <= to).Distinct().Count();
        return Math.Round((decimal)uniqueDays / totalDays * 100m, 1);
    }

    public static Dictionary<string, int> CalculateVolumeDistribution(
        IEnumerable<(string MuscleGroup, int Sets)> data)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (muscleGroup, sets) in data)
        {
            if (result.ContainsKey(muscleGroup))
                result[muscleGroup] += sets;
            else
                result[muscleGroup] = sets;
        }

        return result;
    }
}
