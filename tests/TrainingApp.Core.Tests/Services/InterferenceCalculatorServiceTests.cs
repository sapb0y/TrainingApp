using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class InterferenceCalculatorServiceTests
{
    // ── CalculateCardioTrimp ──

    [Fact]
    public void CalculateCardioTrimp_RunningZ2_30Min()
    {
        var result = InterferenceCalculatorService.CalculateCardioTrimp(
            CardioModality.Running, CardioIntensityZone.Zone2, 30);

        // 30 * 0.8 * 2.5 / 60 = 1.0
        result.Trimp.Should().Be(1.0m);
        result.ZoneLabel.Should().Be("Aerobic");
    }

    [Fact]
    public void CalculateCardioTrimp_CyclingZ4_60Min()
    {
        var result = InterferenceCalculatorService.CalculateCardioTrimp(
            CardioModality.Cycling, CardioIntensityZone.Zone4, 60);

        // 60 * 1.5 * 1.0 / 60 = 1.5
        result.Trimp.Should().Be(1.5m);
        result.ZoneLabel.Should().Be("Threshold");
    }

    [Fact]
    public void CalculateCardioTrimp_WalkingZ1_45Min()
    {
        var result = InterferenceCalculatorService.CalculateCardioTrimp(
            CardioModality.Walking, CardioIntensityZone.Zone1, 45);

        // 45 * 0.5 * 0.5 / 60 = 0.1875 → 0.19
        result.Trimp.Should().Be(0.19m);
        result.ZoneLabel.Should().Be("Recovery");
    }

    [Fact]
    public void CalculateCardioTrimp_SwimmingZ5_20Min()
    {
        var result = InterferenceCalculatorService.CalculateCardioTrimp(
            CardioModality.Swimming, CardioIntensityZone.Zone5, 20);

        // 20 * 2.0 * 1.2 / 60 = 0.8
        result.Trimp.Should().Be(0.8m);
        result.ZoneLabel.Should().Be("VO2max");
    }

    // ── CalculateInterferenceScore ──

    [Fact]
    public void CalculateInterferenceScore_RunningPlusLegs_HighInterference()
    {
        var result = InterferenceCalculatorService.CalculateInterferenceScore(
            CardioModality.Running, CardioIntensityZone.Zone4, 45,
            new List<string> { "quadriceps", "hamstrings" });

        // 2.5 * 1.3 * 1.0 * 1.5 = 4.875
        result.Score.Should().BeGreaterThanOrEqualTo(4m);
        result.Level.Should().Be("Moderate");
    }

    [Fact]
    public void CalculateInterferenceScore_CyclingPlusUpper_LowInterference()
    {
        var result = InterferenceCalculatorService.CalculateInterferenceScore(
            CardioModality.Cycling, CardioIntensityZone.Zone2, 30,
            new List<string> { "chest", "shoulders" });

        // 1.0 * 0.5 * 0.8 * 0.5 = 0.2
        result.Score.Should().BeLessThan(3m);
        result.Level.Should().Be("Low");
    }

    [Fact]
    public void CalculateInterferenceScore_LongZ4Running_HighScore()
    {
        var result = InterferenceCalculatorService.CalculateInterferenceScore(
            CardioModality.Running, CardioIntensityZone.Zone4, 90,
            new List<string> { "quadriceps" });

        // 2.5 * 1.3 * 1.5 * 1.5 = 7.3125
        result.Score.Should().BeGreaterThanOrEqualTo(7m);
        result.Level.Should().Be("High");
    }

    [Fact]
    public void CalculateInterferenceScore_ScoreClampedTo10()
    {
        var result = InterferenceCalculatorService.CalculateInterferenceScore(
            CardioModality.Running, CardioIntensityZone.Zone5, 120,
            new List<string> { "quadriceps", "glutes" });

        result.Score.Should().BeLessThanOrEqualTo(10m);
    }

    [Fact]
    public void CalculateInterferenceScore_NoMuscleOverlap_LowFactor()
    {
        var result = InterferenceCalculatorService.CalculateInterferenceScore(
            CardioModality.Running, CardioIntensityZone.Zone2, 30,
            new List<string>());

        // No muscles → overlapFactor=0.5: 2.5 * 0.5 * 0.8 * 0.5 = 0.5
        result.Score.Should().BeLessThan(1m);
    }

    // ── RecommendSequencing ──

    [Fact]
    public void RecommendSequencing_StrengthPlusZ5_SeparateDays()
    {
        var result = InterferenceCalculatorService.RecommendSequencing(
            true, true, CardioModality.Cycling, CardioIntensityZone.Zone5);

        result.SeparationHours.Should().Be(6);
        result.RecommendedOrder.Should().Contain("Separate");
    }

    [Fact]
    public void RecommendSequencing_StrengthPlusZ1_SameSession()
    {
        var result = InterferenceCalculatorService.RecommendSequencing(
            true, true, CardioModality.Cycling, CardioIntensityZone.Zone1);

        result.SeparationHours.Should().Be(3);
        result.RecommendedOrder.Should().Contain("Strength first");
    }

    [Fact]
    public void RecommendSequencing_NoStrength_AnyOrder()
    {
        var result = InterferenceCalculatorService.RecommendSequencing(
            false, true, CardioModality.Running, CardioIntensityZone.Zone3);

        result.RecommendedOrder.Should().Be("Any");
    }

    [Fact]
    public void RecommendSequencing_RunningZ3WithStrength_Separate()
    {
        var result = InterferenceCalculatorService.RecommendSequencing(
            true, true, CardioModality.Running, CardioIntensityZone.Zone3);

        result.SeparationHours.Should().Be(6);
    }

    // ── CalculateHeartRateZone ──

    [Theory]
    [InlineData(100, 200, CardioIntensityZone.Zone1)]   // 50%
    [InlineData(120, 200, CardioIntensityZone.Zone2)]   // 60%
    [InlineData(140, 200, CardioIntensityZone.Zone3)]   // 70%
    [InlineData(160, 200, CardioIntensityZone.Zone4)]   // 80%
    [InlineData(185, 200, CardioIntensityZone.Zone5)]   // 92.5%
    public void CalculateHeartRateZone_CorrectBoundaries(int hr, int maxHr, CardioIntensityZone expected)
    {
        var result = InterferenceCalculatorService.CalculateHeartRateZone(hr, maxHr);
        result.Should().Be(expected);
    }

    // ── EstimateMaxHr ──

    [Theory]
    [InlineData(30, 187)]  // 208 - 21 = 187
    [InlineData(50, 173)]  // 208 - 35 = 173
    [InlineData(20, 194)]  // 208 - 14 = 194
    public void EstimateMaxHr_TanakaFormula(int age, int expected)
    {
        InterferenceCalculatorService.EstimateMaxHr(age).Should().Be(expected);
    }

    // ── CalculateWeeklySummary ──

    [Fact]
    public void CalculateWeeklySummary_AggregatesCorrectly()
    {
        var sessions = new List<(CardioModality, CardioIntensityZone, int, decimal?, decimal)>
        {
            (CardioModality.Running, CardioIntensityZone.Zone2, 30, 5.0m, 1.0m),
            (CardioModality.Cycling, CardioIntensityZone.Zone3, 45, 15.0m, 0.75m),
            (CardioModality.Running, CardioIntensityZone.Zone2, 20, 3.0m, 0.67m)
        };

        var result = InterferenceCalculatorService.CalculateWeeklySummary(sessions);

        result.TotalSessions.Should().Be(3);
        result.TotalMinutes.Should().Be(95);
        result.TotalDistanceKm.Should().Be(23.0m);
        result.TotalTrimp.Should().Be(2.42m);
        result.MinutesByZone.Should().ContainKey("Zone2").WhoseValue.Should().Be(50);
        result.MinutesByZone.Should().ContainKey("Zone3").WhoseValue.Should().Be(45);
    }

    // ── CalculateDailySummary ──

    [Fact]
    public void CalculateDailySummary_CombinesMetrics()
    {
        var result = InterferenceCalculatorService.CalculateDailySummary(
            strengthTrimp: 15.0m, strengthCount: 1,
            cardioTrimp: 2.5m, cardioCount: 1,
            interferenceScore: 4.5m, interferenceLevel: "Moderate");

        result.StrengthSessions.Should().Be(1);
        result.CardioSessions.Should().Be(1);
        result.TotalTrimp.Should().Be(17.5m);
        result.InterferenceScore.Should().Be(4.5m);
    }
}
