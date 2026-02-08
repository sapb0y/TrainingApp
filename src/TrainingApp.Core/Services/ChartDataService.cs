namespace TrainingApp.Core.Services;

public static class ChartDataService
{
    public record DataPoint(DateOnly Date, decimal Value, string? Label = null);
    public record ChartSeries(string Name, string Unit, List<DataPoint> Points);
    public record ChartStatistics(decimal Min, decimal Max, decimal Average, decimal? Current, decimal? ChangePercent);
    public record ChartData(ChartSeries Series, ChartStatistics Statistics);
    public record MultiSeriesChartData(List<ChartSeries> Series, string Title);

    public record StrengthSnapshot(int ExercisesTracked, decimal? TopE1rm, string? TopExercise, decimal? E1rmChangePct30d);
    public record BodyWeightSnapshot(decimal? CurrentKg, decimal? MovingAvg7d, decimal? WeeklyRateKg, string? Trend);
    public record CardioSnapshot(int SessionsLast30d, int MinutesLast30d, decimal? TotalDistanceKm30d, decimal? AvgTrimp);
    public record FatigueSnapshot(decimal? Ctl, decimal? Atl, decimal? Tsb, string? ReadinessCategory);
    public record VolumeSnapshot(int TotalSetsLast7d, decimal? TotalVolumeLast7d, decimal? VolumeChangePctVsPrev7d);
    public record DashboardSummary(
        StrengthSnapshot Strength, BodyWeightSnapshot BodyWeight,
        CardioSnapshot Cardio, FatigueSnapshot Fatigue,
        VolumeSnapshot Volume, int ActiveDaysLast30);

    public static ChartData BuildStrengthProgressionChart(IEnumerable<(DateOnly Date, decimal E1rm)> points)
    {
        var list = points.OrderBy(p => p.Date).ToList();
        var dataPoints = list.Select(p => new DataPoint(p.Date, Math.Round(p.E1rm, 1))).ToList();
        var series = new ChartSeries("e1RM", "kg", dataPoints);
        var stats = CalculateStatistics(dataPoints);
        return new ChartData(series, stats);
    }

    public static MultiSeriesChartData BuildBodyWeightChart(
        IEnumerable<(DateOnly Date, decimal WeightKg, decimal? Ma7d, decimal? Ma30d)> points)
    {
        var list = points.OrderBy(p => p.Date).ToList();

        var rawSeries = new ChartSeries("Weight", "kg",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.WeightKg, 1))).ToList());

        var ma7Series = new ChartSeries("7d Moving Avg", "kg",
            list.Where(p => p.Ma7d.HasValue)
                .Select(p => new DataPoint(p.Date, Math.Round(p.Ma7d!.Value, 1))).ToList());

        var ma30Series = new ChartSeries("30d Moving Avg", "kg",
            list.Where(p => p.Ma30d.HasValue)
                .Select(p => new DataPoint(p.Date, Math.Round(p.Ma30d!.Value, 1))).ToList());

        return new MultiSeriesChartData([rawSeries, ma7Series, ma30Series], "Body Weight");
    }

    public static MultiSeriesChartData BuildVolumeChart(
        IEnumerable<(DateOnly Date, decimal TotalVolume, int TotalSets)> points)
    {
        var list = points.OrderBy(p => p.Date).ToList();

        var volumeSeries = new ChartSeries("Total Volume", "kg",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.TotalVolume, 0))).ToList());

        var setsSeries = new ChartSeries("Total Sets", "sets",
            list.Select(p => new DataPoint(p.Date, p.TotalSets)).ToList());

        return new MultiSeriesChartData([volumeSeries, setsSeries], "Training Volume");
    }

    public static MultiSeriesChartData BuildCardioChart(
        IEnumerable<(DateOnly Date, int DurationMin, decimal? DistanceKm, decimal Trimp)> points)
    {
        var list = points.OrderBy(p => p.Date).ToList();

        var durationSeries = new ChartSeries("Duration", "min",
            list.Select(p => new DataPoint(p.Date, p.DurationMin)).ToList());

        var distanceSeries = new ChartSeries("Distance", "km",
            list.Where(p => p.DistanceKm.HasValue)
                .Select(p => new DataPoint(p.Date, Math.Round(p.DistanceKm!.Value, 1))).ToList());

        var trimpSeries = new ChartSeries("TRIMP", "au",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.Trimp, 1))).ToList());

        return new MultiSeriesChartData([durationSeries, distanceSeries, trimpSeries], "Cardio");
    }

    public static MultiSeriesChartData BuildFatigueChart(
        IEnumerable<(DateOnly Date, decimal Ctl, decimal Atl, decimal Tsb)> points)
    {
        var list = points.OrderBy(p => p.Date).ToList();

        var ctlSeries = new ChartSeries("CTL (Fitness)", "au",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.Ctl, 1))).ToList());

        var atlSeries = new ChartSeries("ATL (Fatigue)", "au",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.Atl, 1))).ToList());

        var tsbSeries = new ChartSeries("TSB (Form)", "au",
            list.Select(p => new DataPoint(p.Date, Math.Round(p.Tsb, 1))).ToList());

        return new MultiSeriesChartData([ctlSeries, atlSeries, tsbSeries], "Fatigue Model");
    }

    public static DashboardSummary CalculateDashboardSummary(
        StrengthSnapshot strength, BodyWeightSnapshot bodyWeight,
        CardioSnapshot cardio, FatigueSnapshot fatigue,
        VolumeSnapshot volume, int activeDaysLast30)
    {
        return new DashboardSummary(strength, bodyWeight, cardio, fatigue, volume, activeDaysLast30);
    }

    private static ChartStatistics CalculateStatistics(List<DataPoint> points)
    {
        if (points.Count == 0)
            return new ChartStatistics(0, 0, 0, null, null);

        var values = points.Select(p => p.Value).ToList();
        var min = values.Min();
        var max = values.Max();
        var avg = Math.Round(values.Average(), 1);
        var current = values.Last();

        decimal? changePct = null;
        if (points.Count >= 2)
        {
            var first = values.First();
            if (first != 0)
                changePct = Math.Round((current - first) / first * 100m, 1);
        }

        return new ChartStatistics(min, max, avg, current, changePct);
    }
}
