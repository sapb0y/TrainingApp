using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class ChartDataServiceTests
{
    // ── BuildStrengthProgressionChart ──

    [Fact]
    public void BuildStrengthProgressionChart_Empty_ReturnsEmptyChart()
    {
        var result = ChartDataService.BuildStrengthProgressionChart([]);

        result.Series.Points.Should().BeEmpty();
        result.Statistics.Min.Should().Be(0);
        result.Statistics.Current.Should().BeNull();
    }

    [Fact]
    public void BuildStrengthProgressionChart_SinglePoint_ReturnsCorrectStats()
    {
        var points = new[] { (new DateOnly(2025, 1, 1), 100m) };

        var result = ChartDataService.BuildStrengthProgressionChart(points);

        result.Series.Name.Should().Be("e1RM");
        result.Series.Unit.Should().Be("kg");
        result.Series.Points.Should().HaveCount(1);
        result.Statistics.Min.Should().Be(100m);
        result.Statistics.Max.Should().Be(100m);
        result.Statistics.Current.Should().Be(100m);
        result.Statistics.ChangePercent.Should().BeNull();
    }

    [Fact]
    public void BuildStrengthProgressionChart_MultiplePoints_CalculatesChangePercent()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 100m),
            (new DateOnly(2025, 1, 15), 105m),
            (new DateOnly(2025, 2, 1), 110m)
        };

        var result = ChartDataService.BuildStrengthProgressionChart(points);

        result.Series.Points.Should().HaveCount(3);
        result.Statistics.Min.Should().Be(100m);
        result.Statistics.Max.Should().Be(110m);
        result.Statistics.Current.Should().Be(110m);
        result.Statistics.ChangePercent.Should().Be(10m); // (110-100)/100 * 100
    }

    [Fact]
    public void BuildStrengthProgressionChart_UnsortedInput_SortsByDate()
    {
        var points = new[]
        {
            (new DateOnly(2025, 2, 1), 110m),
            (new DateOnly(2025, 1, 1), 100m),
            (new DateOnly(2025, 1, 15), 105m)
        };

        var result = ChartDataService.BuildStrengthProgressionChart(points);

        result.Series.Points[0].Date.Should().Be(new DateOnly(2025, 1, 1));
        result.Series.Points[2].Date.Should().Be(new DateOnly(2025, 2, 1));
    }

    // ── BuildBodyWeightChart ──

    [Fact]
    public void BuildBodyWeightChart_Produces3Series()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 80m, (decimal?)79.5m, (decimal?)80.0m),
            (new DateOnly(2025, 1, 2), 79.8m, (decimal?)79.6m, (decimal?)79.9m)
        };

        var result = ChartDataService.BuildBodyWeightChart(points);

        result.Title.Should().Be("Body Weight");
        result.Series.Should().HaveCount(3);
        result.Series[0].Name.Should().Be("Weight");
        result.Series[1].Name.Should().Be("7d Moving Avg");
        result.Series[2].Name.Should().Be("30d Moving Avg");
    }

    [Fact]
    public void BuildBodyWeightChart_FiltersNullMovingAverages()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 80m, (decimal?)null, (decimal?)null),
            (new DateOnly(2025, 1, 2), 79.8m, (decimal?)79.9m, (decimal?)null)
        };

        var result = ChartDataService.BuildBodyWeightChart(points);

        result.Series[0].Points.Should().HaveCount(2); // raw always present
        result.Series[1].Points.Should().HaveCount(1); // only non-null MA7d
        result.Series[2].Points.Should().BeEmpty();     // no MA30d
    }

    // ── BuildVolumeChart ──

    [Fact]
    public void BuildVolumeChart_Produces2Series()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 5000m, 20),
            (new DateOnly(2025, 1, 2), 6000m, 24)
        };

        var result = ChartDataService.BuildVolumeChart(points);

        result.Title.Should().Be("Training Volume");
        result.Series.Should().HaveCount(2);
        result.Series[0].Name.Should().Be("Total Volume");
        result.Series[1].Name.Should().Be("Total Sets");
    }

    // ── BuildCardioChart ──

    [Fact]
    public void BuildCardioChart_Produces3Series()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 30, (decimal?)5.0m, 120m),
            (new DateOnly(2025, 1, 2), 45, (decimal?)null, 180m)
        };

        var result = ChartDataService.BuildCardioChart(points);

        result.Title.Should().Be("Cardio");
        result.Series.Should().HaveCount(3);
        result.Series[0].Name.Should().Be("Duration");
        result.Series[1].Name.Should().Be("Distance");
        result.Series[2].Name.Should().Be("TRIMP");
        result.Series[1].Points.Should().HaveCount(1); // null distance filtered
    }

    // ── BuildFatigueChart ──

    [Fact]
    public void BuildFatigueChart_Produces3Series_CorrectNames()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 30m, 25m, 5m),
            (new DateOnly(2025, 1, 2), 31m, 28m, 3m)
        };

        var result = ChartDataService.BuildFatigueChart(points);

        result.Title.Should().Be("Fatigue Model");
        result.Series.Should().HaveCount(3);
        result.Series[0].Name.Should().Be("CTL (Fitness)");
        result.Series[1].Name.Should().Be("ATL (Fatigue)");
        result.Series[2].Name.Should().Be("TSB (Form)");
        result.Series[0].Points.Should().HaveCount(2);
    }

    // ── CalculateDashboardSummary ──

    [Fact]
    public void CalculateDashboardSummary_AssemblesComposite()
    {
        var strength = new ChartDataService.StrengthSnapshot(5, 120m, "Bench Press", 5.0m);
        var bodyWeight = new ChartDataService.BodyWeightSnapshot(80m, 79.5m, -0.3m, "Decreasing");
        var cardio = new ChartDataService.CardioSnapshot(8, 240, 30.0m, 95m);
        var fatigue = new ChartDataService.FatigueSnapshot(35m, 28m, 7m, "Good");
        var volume = new ChartDataService.VolumeSnapshot(30, 15000m, 10.5m);

        var result = ChartDataService.CalculateDashboardSummary(
            strength, bodyWeight, cardio, fatigue, volume, 20);

        result.Strength.Should().Be(strength);
        result.BodyWeight.Should().Be(bodyWeight);
        result.Cardio.Should().Be(cardio);
        result.Fatigue.Should().Be(fatigue);
        result.Volume.Should().Be(volume);
        result.ActiveDaysLast30.Should().Be(20);
    }

    // ── BuildStrengthProgressionChart: Average calculation ──

    [Fact]
    public void BuildStrengthProgressionChart_AverageIsCorrect()
    {
        var points = new[]
        {
            (new DateOnly(2025, 1, 1), 100m),
            (new DateOnly(2025, 1, 2), 200m),
            (new DateOnly(2025, 1, 3), 300m)
        };

        var result = ChartDataService.BuildStrengthProgressionChart(points);

        result.Statistics.Average.Should().Be(200m);
    }
}
