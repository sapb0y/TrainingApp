using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class TrainingStressServiceTests
{
    // --- EstimateRpe ---

    [Fact]
    public void EstimateRpe_ReturnsRpe_WhenProvided()
    {
        TrainingStressService.EstimateRpe(7.5m, rir: 2).Should().Be(7.5m);
    }

    [Fact]
    public void EstimateRpe_CalculatesFromRir_WhenNoRpe()
    {
        TrainingStressService.EstimateRpe(null, rir: 2).Should().Be(8m);
        TrainingStressService.EstimateRpe(null, rir: 0).Should().Be(10m);
    }

    [Fact]
    public void EstimateRpe_ReturnsDefault8_WhenBothNull()
    {
        TrainingStressService.EstimateRpe(null, null).Should().Be(8m);
    }

    [Fact]
    public void EstimateRpe_ClampsTo1And10()
    {
        TrainingStressService.EstimateRpe(0m, null).Should().Be(1m);
        TrainingStressService.EstimateRpe(15m, null).Should().Be(10m);
        TrainingStressService.EstimateRpe(null, rir: -5).Should().Be(10m);
        TrainingStressService.EstimateRpe(null, rir: 20).Should().Be(1m);
    }

    // --- CalculateSetStress ---

    [Fact]
    public void CalculateSetStress_WarmupReturnsZero()
    {
        var result = TrainingStressService.CalculateSetStress(100m, 10, 8m, null, isWarmup: true);
        result.Trimp.Should().Be(0);
    }

    [Fact]
    public void CalculateSetStress_NullWeightReturnsZero()
    {
        var result = TrainingStressService.CalculateSetStress(null, 10, 8m, null, false);
        result.Trimp.Should().Be(0);
    }

    [Fact]
    public void CalculateSetStress_NullRepsReturnsZero()
    {
        var result = TrainingStressService.CalculateSetStress(100m, null, 8m, null, false);
        result.Trimp.Should().Be(0);
    }

    [Fact]
    public void CalculateSetStress_ZeroWeightReturnsZero()
    {
        var result = TrainingStressService.CalculateSetStress(0m, 10, 8m, null, false);
        result.Trimp.Should().Be(0);
    }

    [Fact]
    public void CalculateSetStress_CorrectTrimpWithDivide100()
    {
        // 100kg × 10reps × (8/10) / 100 = 8.0
        var result = TrainingStressService.CalculateSetStress(100m, 10, 8m, null, false);
        result.Trimp.Should().Be(8.0m);
        result.Weight.Should().Be(100m);
        result.Reps.Should().Be(10);
        result.Rpe.Should().Be(8m);
    }

    [Fact]
    public void CalculateSetStress_UsesRirWhenNoRpe()
    {
        // 80kg × 8reps × ((10-2)/10) / 100 = 80*8*0.8/100 = 5.12
        var result = TrainingStressService.CalculateSetStress(80m, 8, null, 2, false);
        result.Trimp.Should().Be(5.12m);
    }

    [Fact]
    public void CalculateSetStress_MaxEffortHighTrimp()
    {
        // 200kg × 1rep × (10/10) / 100 = 2.0
        var result = TrainingStressService.CalculateSetStress(200m, 1, 10m, null, false);
        result.Trimp.Should().Be(2.0m);
    }

    // --- CalculateSessionStress ---

    [Fact]
    public void CalculateSessionStress_AggregatesNonWarmupSets()
    {
        var sets = new List<(decimal?, int?, decimal?, int?, bool)>
        {
            (60m, 10, null, null, true),  // warmup → excluded
            (100m, 10, 8m, null, false),  // 8.0 TRIMP
            (100m, 8, 9m, null, false),   // 7.2 TRIMP
        };

        var result = TrainingStressService.CalculateSessionStress(sets);
        result.TotalSets.Should().Be(2);
        result.TotalReps.Should().Be(18);
        result.TotalVolume.Should().Be(1800m); // 100*10 + 100*8
        result.Trimp.Should().Be(15.2m);       // 8.0 + 7.2
        result.AverageRpe.Should().Be(8.5m);
    }

    [Fact]
    public void CalculateSessionStress_EmptySetsReturnsZero()
    {
        var result = TrainingStressService.CalculateSessionStress([]);
        result.Trimp.Should().Be(0);
        result.TotalSets.Should().Be(0);
    }

    [Fact]
    public void CalculateSessionStress_AllWarmupsReturnsZero()
    {
        var sets = new List<(decimal?, int?, decimal?, int?, bool)>
        {
            (40m, 10, 5m, null, true),
            (60m, 5, 6m, null, true),
        };
        var result = TrainingStressService.CalculateSessionStress(sets);
        result.Trimp.Should().Be(0);
    }

    // --- CalculateDailyStress ---

    [Fact]
    public void CalculateDailyStress_TwoSessionsAggregate()
    {
        var sessions = new List<TrainingStressService.SessionStress>
        {
            new(10m, 4, 40, 4000m, 8m),
            new(8m, 3, 24, 2400m, 7m),
        };
        var rpes = new List<int?> { 8, 7 };

        var result = TrainingStressService.CalculateDailyStress(sessions, rpes);
        result.Trimp.Should().Be(18m);
        result.TotalSets.Should().Be(7);
        result.TotalReps.Should().Be(64);
        result.TotalVolume.Should().Be(6400m);
        result.WorkoutCount.Should().Be(2);
        result.AverageSessionRpe.Should().Be(7.5m);
    }

    [Fact]
    public void CalculateDailyStress_RestDayReturnsZero()
    {
        var result = TrainingStressService.CalculateDailyStress([], []);
        result.Trimp.Should().Be(0);
        result.WorkoutCount.Should().Be(0);
        result.AverageSessionRpe.Should().BeNull();
    }

    // --- UpdateFatigueState ---

    [Fact]
    public void UpdateFatigueState_TrainingDayIncreasesBoth()
    {
        var result = TrainingStressService.UpdateFatigueState(0m, 0m, 50m);
        result.Ctl.Should().BeGreaterThan(0);
        result.Atl.Should().BeGreaterThan(0);
    }

    [Fact]
    public void UpdateFatigueState_RestDayDecaysBoth()
    {
        var result = TrainingStressService.UpdateFatigueState(50m, 50m, 0m);
        result.Ctl.Should().BeLessThan(50m);
        result.Atl.Should().BeLessThan(50m);
    }

    [Fact]
    public void UpdateFatigueState_AtlDecaysFasterThanCtl()
    {
        var result = TrainingStressService.UpdateFatigueState(50m, 50m, 0m);
        // ATL decays with τ=7 (faster), CTL with τ=42 (slower)
        var ctlDecay = 50m - result.Ctl;
        var atlDecay = 50m - result.Atl;
        atlDecay.Should().BeGreaterThan(ctlDecay);
    }

    [Fact]
    public void UpdateFatigueState_TsbEqualsCtlMinusAtl()
    {
        var result = TrainingStressService.UpdateFatigueState(30m, 10m, 20m);
        result.Tsb.Should().Be(result.Ctl - result.Atl);
    }

    [Fact]
    public void UpdateFatigueState_MultiDayConvergence()
    {
        // Simulate 200 days of consistent 20 TRIMP → CTL should approach 20
        var ctl = 0m;
        var atl = 0m;
        for (var i = 0; i < 200; i++)
        {
            var state = TrainingStressService.UpdateFatigueState(ctl, atl, 20m);
            ctl = state.Ctl;
            atl = state.Atl;
        }

        ctl.Should().BeApproximately(20m, 0.5m);
        atl.Should().BeApproximately(20m, 0.01m);
    }

    [Fact]
    public void UpdateFatigueState_FromZeroSingleDay()
    {
        var result = TrainingStressService.UpdateFatigueState(0m, 0m, 100m);
        // CTL alpha ≈ 0.0235, ATL alpha ≈ 0.1331
        result.Ctl.Should().BeApproximately(2.35m, 0.1m);
        result.Atl.Should().BeApproximately(13.31m, 0.1m);
        result.Tsb.Should().BeLessThan(0); // negative after first big session
    }

    // --- CalculateReadiness ---

    [Fact]
    public void CalculateReadiness_FreshAtHighTsb()
    {
        var result = TrainingStressService.CalculateReadiness(20m, RecoveryCapacity.Normal);
        result.Score.Should().BeGreaterThanOrEqualTo(7m);
        result.Category.Should().BeOneOf("Good", "Excellent");
    }

    [Fact]
    public void CalculateReadiness_OverreachedAtLowTsb()
    {
        var result = TrainingStressService.CalculateReadiness(-25m, RecoveryCapacity.Normal);
        result.Score.Should().BeLessThan(5m);
    }

    [Fact]
    public void CalculateReadiness_NeutralAtZero()
    {
        var result = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal);
        result.Score.Should().BeApproximately(5.5m, 0.5m);
    }

    [Fact]
    public void CalculateReadiness_RecoveryLogBoosts()
    {
        var baseline = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal);
        var boosted = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal,
            sleepQuality: 5, stressLevel: 1, energyLevel: 5, muscleReadiness: 5, mood: 5);
        boosted.Score.Should().BeGreaterThan(baseline.Score);
    }

    [Fact]
    public void CalculateReadiness_RecoveryLogReduces()
    {
        var baseline = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal);
        var reduced = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal,
            sleepQuality: 1, stressLevel: 5, energyLevel: 1, muscleReadiness: 1, mood: 1);
        reduced.Score.Should().BeLessThan(baseline.Score);
    }

    [Fact]
    public void CalculateReadiness_HighCapacityBoosts()
    {
        var normal = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal);
        var high = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.High);
        high.Score.Should().BeGreaterThan(normal.Score);
    }

    [Fact]
    public void CalculateReadiness_LowCapacityReduces()
    {
        var normal = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Normal);
        var low = TrainingStressService.CalculateReadiness(0m, RecoveryCapacity.Low);
        low.Score.Should().BeLessThan(normal.Score);
    }

    [Fact]
    public void CalculateReadiness_ScoreClamped1To10()
    {
        var result = TrainingStressService.CalculateReadiness(100m, RecoveryCapacity.High,
            sleepQuality: 5, stressLevel: 1, energyLevel: 5, muscleReadiness: 5, mood: 5);
        result.Score.Should().BeLessThanOrEqualTo(10m);

        var low = TrainingStressService.CalculateReadiness(-100m, RecoveryCapacity.Low,
            sleepQuality: 1, stressLevel: 5, energyLevel: 1, muscleReadiness: 1, mood: 1);
        low.Score.Should().BeGreaterThanOrEqualTo(1m);
    }
}
