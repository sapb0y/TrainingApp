using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class ProgressAnalyticsServiceTests
{
    // ── DetectTrend ──

    [Fact]
    public void DetectTrend_Increasing_ReturnsIncreasing()
    {
        var points = Enumerable.Range(0, 14)
            .Select(i => (new DateOnly(2025, 1, 1).AddDays(i), 100m + i * 2m))
            .ToList();

        var result = ProgressAnalyticsService.DetectTrend(points);

        result.Should().Be("Increasing");
    }

    [Fact]
    public void DetectTrend_Decreasing_ReturnsDecreasing()
    {
        var points = Enumerable.Range(0, 14)
            .Select(i => (new DateOnly(2025, 1, 1).AddDays(i), 100m - i * 2m))
            .ToList();

        var result = ProgressAnalyticsService.DetectTrend(points);

        result.Should().Be("Decreasing");
    }

    [Fact]
    public void DetectTrend_Stable_ReturnsStable()
    {
        var points = Enumerable.Range(0, 14)
            .Select(i => (new DateOnly(2025, 1, 1).AddDays(i), 100m))
            .ToList();

        var result = ProgressAnalyticsService.DetectTrend(points);

        result.Should().Be("Stable");
    }

    [Fact]
    public void DetectTrend_InsufficientData_ReturnsInsufficientData()
    {
        var points = new[] { (new DateOnly(2025, 1, 1), 100m) };

        var result = ProgressAnalyticsService.DetectTrend(points);

        result.Should().Be("Insufficient Data");
    }

    // ── FindPersonalRecords ──

    [Fact]
    public void FindPersonalRecords_FindsBestPerExercise()
    {
        var history = new[]
        {
            (new DateOnly(2025, 1, 1), 100m, "Bench Press"),
            (new DateOnly(2025, 1, 5), 105m, "Bench Press"),
            (new DateOnly(2025, 1, 1), 150m, "Squat"),
            (new DateOnly(2025, 1, 5), 140m, "Squat")
        };

        var result = ProgressAnalyticsService.FindPersonalRecords(history);

        result.Should().HaveCount(2);
        result[0].ExerciseName.Should().Be("Squat");
        result[0].BestE1rm.Should().Be(150m);
        result[1].ExerciseName.Should().Be("Bench Press");
        result[1].BestE1rm.Should().Be(105m);
    }

    [Fact]
    public void FindPersonalRecords_MarksRecentCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var history = new[]
        {
            (today.AddDays(-3), 100m, "Bench Press"),
            (today.AddDays(-30), 150m, "Squat")
        };

        var result = ProgressAnalyticsService.FindPersonalRecords(history);

        result.First(r => r.ExerciseName == "Bench Press").IsRecent.Should().BeTrue();
        result.First(r => r.ExerciseName == "Squat").IsRecent.Should().BeFalse();
    }

    // ── CalculateConsistency ──

    [Fact]
    public void CalculateConsistency_AllDaysActive_Returns100()
    {
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 10);
        var dates = Enumerable.Range(0, 10).Select(i => from.AddDays(i));

        var result = ProgressAnalyticsService.CalculateConsistency(dates, from, to);

        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateConsistency_HalfDaysActive_Returns50()
    {
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 10);
        var dates = Enumerable.Range(0, 5).Select(i => from.AddDays(i));

        var result = ProgressAnalyticsService.CalculateConsistency(dates, from, to);

        result.Should().Be(50m);
    }

    [Fact]
    public void CalculateConsistency_NoDatesActive_Returns0()
    {
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 10);

        var result = ProgressAnalyticsService.CalculateConsistency([], from, to);

        result.Should().Be(0m);
    }

    // ── CalculateVolumeDistribution ──

    [Fact]
    public void CalculateVolumeDistribution_AggregatesCorrectly()
    {
        var data = new[]
        {
            ("Chest", 10),
            ("Back", 8),
            ("Chest", 6),
            ("Legs", 12)
        };

        var result = ProgressAnalyticsService.CalculateVolumeDistribution(data);

        result.Should().HaveCount(3);
        result["Chest"].Should().Be(16);
        result["Back"].Should().Be(8);
        result["Legs"].Should().Be(12);
    }
}
