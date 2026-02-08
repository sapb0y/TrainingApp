using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class InsightRuleEngineTests
{
    // ── EvaluateStrengthInsights ──

    [Fact]
    public void EvaluateStrength_NewPR_DetectsAchievement()
    {
        var exercises = new[]
        {
            ("Bench Press", Guid.NewGuid(), 105m, 100m, 100m, 5) // current > allTime
        };

        var result = InsightRuleEngine.EvaluateStrengthInsights(exercises);

        result.Should().ContainSingle(r => r.RuleName == "New PR");
        result.First(r => r.RuleName == "New PR").Severity.Should().Be("Achievement");
    }

    [Fact]
    public void EvaluateStrength_Plateau_DetectsWarning()
    {
        var exercises = new[]
        {
            ("Squat", Guid.NewGuid(), 100m, 99.5m, 105m, 6) // <2% change, >=4 sessions
        };

        var result = InsightRuleEngine.EvaluateStrengthInsights(exercises);

        result.Should().ContainSingle(r => r.RuleName == "Strength Plateau");
        result.First(r => r.RuleName == "Strength Plateau").Severity.Should().Be("Warning");
    }

    [Fact]
    public void EvaluateStrength_RapidProgression_DetectsInfo()
    {
        var exercises = new[]
        {
            ("Deadlift", Guid.NewGuid(), 120m, 100m, 120m, 8) // >10% increase
        };

        var result = InsightRuleEngine.EvaluateStrengthInsights(exercises);

        result.Should().Contain(r => r.RuleName == "Rapid Progression");
    }

    [Fact]
    public void EvaluateStrength_NormalProgress_NoInsight()
    {
        var exercises = new[]
        {
            ("Bench Press", Guid.NewGuid(), 102m, 100m, 105m, 3) // 2% change, <4 sessions
        };

        var result = InsightRuleEngine.EvaluateStrengthInsights(exercises);

        result.Should().NotContain(r => r.RuleName == "Strength Plateau");
    }

    // ── EvaluateVolumeInsights ──

    [Fact]
    public void EvaluateVolume_Spike_DetectsWarning()
    {
        var result = InsightRuleEngine.EvaluateVolumeInsights(
            13000m, 10000m, [], 3); // 30% increase

        result.Should().ContainSingle(r => r.RuleName == "Volume Spike");
    }

    [Fact]
    public void EvaluateVolume_LowCoverage_DetectsInfo()
    {
        var muscleGroups = new[] { ("Chest", 3, 3) }; // <4 sets, 3+ weeks

        var result = InsightRuleEngine.EvaluateVolumeInsights(10000m, 10000m, muscleGroups, 3);

        result.Should().ContainSingle(r => r.RuleName == "Low Muscle Coverage");
    }

    [Fact]
    public void EvaluateVolume_ConsistentTraining_DetectsAchievement()
    {
        var result = InsightRuleEngine.EvaluateVolumeInsights(10000m, 10000m, [], 4); // >=4 sessions/week

        result.Should().ContainSingle(r => r.RuleName == "Consistent Training");
        result.First(r => r.RuleName == "Consistent Training").Severity.Should().Be("Achievement");
    }

    // ── EvaluateRecoveryInsights ──

    [Fact]
    public void EvaluateRecovery_Overreaching_DetectsWarning()
    {
        var data = Enumerable.Range(0, 5)
            .Select(i => (new DateOnly(2025, 1, 1).AddDays(i), -25m, (decimal?)50m))
            .ToList();

        var result = InsightRuleEngine.EvaluateRecoveryInsights(data);

        result.Should().ContainSingle(r => r.RuleName == "Overreaching");
    }

    [Fact]
    public void EvaluateRecovery_FreshAndReady_DetectsInfo()
    {
        var data = new[] { (new DateOnly(2025, 1, 1), 15m, (decimal?)80m) };

        var result = InsightRuleEngine.EvaluateRecoveryInsights(data);

        result.Should().ContainSingle(r => r.RuleName == "Fresh & Ready");
    }

    // ── EvaluateWeightInsights ──

    [Fact]
    public void EvaluateWeight_GoalReached_DetectsAchievement()
    {
        var result = InsightRuleEngine.EvaluateWeightInsights(74m, 75m, -1m, true, -0.5m, 0.5m);

        result.Should().Contain(r => r.RuleName == "Goal Weight Reached");
    }

    [Fact]
    public void EvaluateWeight_Stall_DetectsWarning()
    {
        var result = InsightRuleEngine.EvaluateWeightInsights(80m, 75m, 0.05m, true, -0.1m, 0.5m);

        result.Should().ContainSingle(r => r.RuleName == "Weight Stall");
    }

    [Fact]
    public void EvaluateWeight_OnTrack_DetectsInfo()
    {
        var result = InsightRuleEngine.EvaluateWeightInsights(78m, 75m, -0.5m, true, -0.45m, 0.5m);

        result.Should().Contain(r => r.RuleName == "On Track");
    }

    [Fact]
    public void EvaluateWeight_NoInsight_WhenInsufficientData()
    {
        var result = InsightRuleEngine.EvaluateWeightInsights(null, null, null, false, null, null);

        result.Should().BeEmpty();
    }
}
