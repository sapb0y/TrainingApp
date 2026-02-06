using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class AutoregulationServiceTests
{
    private const decimal DefaultIncrement = 2.5m;

    // --- EvaluateSet ---

    [Fact]
    public void EvaluateSet_ReducesWeight_WhenDriftNegative2OrMore()
    {
        // actualRir=1, targetRir=3 → drift = -2 → reduce 5%
        var result = AutoregulationService.EvaluateSet(targetRir: 3, actualRir: 1, currentWeight: 100m, weightIncrement: DefaultIncrement);

        result.Load.Should().NotBeNull();
        result.Load!.Type.Should().Be(AutoregulationService.AdjustmentType.Reduce);
        result.Load.AdjustmentPercent.Should().Be(-5m);
        result.Load.RecommendedWeight.Should().Be(95m); // 100 * 0.95 rounded to 2.5
        result.Reason.Should().Contain("too hard");
    }

    [Fact]
    public void EvaluateSet_IncreasesWeight_WhenDriftPositive2OrMore()
    {
        // actualRir=5, targetRir=3 → drift = +2 → increase 5%
        var result = AutoregulationService.EvaluateSet(targetRir: 3, actualRir: 5, currentWeight: 100m, weightIncrement: DefaultIncrement);

        result.Load.Should().NotBeNull();
        result.Load!.Type.Should().Be(AutoregulationService.AdjustmentType.Increase);
        result.Load.AdjustmentPercent.Should().Be(5m);
        result.Load.RecommendedWeight.Should().Be(105m); // 100 * 1.05 rounded to 2.5
        result.Reason.Should().Contain("too easy");
    }

    [Fact]
    public void EvaluateSet_Maintains_WhenDriftWithinRange()
    {
        // actualRir=3, targetRir=2 → drift = +1 → maintain
        var result = AutoregulationService.EvaluateSet(targetRir: 2, actualRir: 3, currentWeight: 80m, weightIncrement: DefaultIncrement);

        result.Load.Should().NotBeNull();
        result.Load!.Type.Should().Be(AutoregulationService.AdjustmentType.Maintain);
        result.Load.RecommendedWeight.Should().Be(80m);
        result.Load.AdjustmentPercent.Should().Be(0m);
    }

    [Fact]
    public void EvaluateSet_ReturnsNull_WhenRirNull()
    {
        var result = AutoregulationService.EvaluateSet(targetRir: 2, actualRir: null, currentWeight: 80m, weightIncrement: DefaultIncrement);

        result.Load.Should().BeNull();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void EvaluateSet_ReturnsNull_WhenTargetRirNull()
    {
        var result = AutoregulationService.EvaluateSet(targetRir: null, actualRir: 3, currentWeight: 80m, weightIncrement: DefaultIncrement);

        result.Load.Should().BeNull();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void EvaluateSet_RoundsToIncrement()
    {
        // 77 * 0.95 = 73.15 → rounded to nearest 2.5 = 72.5
        var result = AutoregulationService.EvaluateSet(targetRir: 3, actualRir: 0, currentWeight: 77m, weightIncrement: DefaultIncrement);

        result.Load.Should().NotBeNull();
        result.Load!.RecommendedWeight.Should().Be(72.5m);
    }

    [Fact]
    public void EvaluateSet_DriftExactlyNegative2_Reduces()
    {
        // actualRir=0, targetRir=2 → drift = -2
        var result = AutoregulationService.EvaluateSet(targetRir: 2, actualRir: 0, currentWeight: 100m, weightIncrement: DefaultIncrement);

        result.Load!.Type.Should().Be(AutoregulationService.AdjustmentType.Reduce);
    }

    [Fact]
    public void EvaluateSet_DriftExactlyPositive2_Increases()
    {
        // actualRir=4, targetRir=2 → drift = +2
        var result = AutoregulationService.EvaluateSet(targetRir: 2, actualRir: 4, currentWeight: 100m, weightIncrement: DefaultIncrement);

        result.Load!.Type.Should().Be(AutoregulationService.AdjustmentType.Increase);
    }

    // --- EvaluateExerciseVolume ---

    [Fact]
    public void EvaluateExerciseVolume_SkipsRemaining_WhenSeverelyFatigued()
    {
        // avgDrift = -3, 3 completed sets
        var sets = new List<(int?, int?)> { (2, 0), (2, 0), (3, 0) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.SkipRemaining);
    }

    [Fact]
    public void EvaluateExerciseVolume_AddsSet_WhenPerformingWell()
    {
        // avgDrift = +2, completed >= target
        var sets = new List<(int?, int?)> { (2, 4), (2, 4), (2, 4), (2, 4) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.AddSet);
    }

    [Fact]
    public void EvaluateExerciseVolume_Continues_WhenNormalDrift()
    {
        var sets = new List<(int?, int?)> { (2, 2), (2, 3), (2, 2) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.Continue);
    }

    [Fact]
    public void EvaluateExerciseVolume_Continues_WhenTooFewSetsForSkip()
    {
        // avgDrift = -3 but only 2 sets → don't skip yet
        var sets = new List<(int?, int?)> { (2, 0), (3, 0) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.Continue);
    }

    [Fact]
    public void EvaluateExerciseVolume_Continues_WhenNotEnoughSetsForAdd()
    {
        // avgDrift = +2 but completed < target
        var sets = new List<(int?, int?)> { (2, 4), (2, 4) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.Continue);
    }

    [Fact]
    public void EvaluateExerciseVolume_Continues_WhenAllNulls()
    {
        var sets = new List<(int?, int?)> { (null, null), (null, 3), (2, null) };
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.Continue);
    }

    [Fact]
    public void EvaluateExerciseVolume_Continues_WhenEmpty()
    {
        var sets = new List<(int?, int?)>();
        var result = AutoregulationService.EvaluateExerciseVolume(sets, targetSets: 4);

        result.Should().Be(AutoregulationService.VolumeAction.Continue);
    }

    // --- EvaluateSession ---

    [Fact]
    public void EvaluateSession_SuggestsDeload_WhenHighRpeTrend()
    {
        var rpes = new List<int?> { 8, 9, 10, 10, 10 };
        var result = AutoregulationService.EvaluateSession(rpes, currentSessionSets: 15);

        result.Should().Contain("deload");
    }

    [Fact]
    public void EvaluateSession_ReturnsNull_WhenNormalRpe()
    {
        var rpes = new List<int?> { 7, 7, 8 };
        var result = AutoregulationService.EvaluateSession(rpes, currentSessionSets: 15);

        result.Should().BeNull();
    }

    [Fact]
    public void EvaluateSession_ReturnsNull_WhenTooFewSessions()
    {
        var rpes = new List<int?> { 10, 10 };
        var result = AutoregulationService.EvaluateSession(rpes, currentSessionSets: 15);

        result.Should().BeNull();
    }

    [Fact]
    public void EvaluateSession_ReturnsNull_WhenAllNull()
    {
        var rpes = new List<int?> { null, null, null };
        var result = AutoregulationService.EvaluateSession(rpes, currentSessionSets: 15);

        result.Should().BeNull();
    }

    [Fact]
    public void EvaluateSession_UsesLastThreeOnly()
    {
        // Old RPEs low, but last 3 are high → suggest deload
        var rpes = new List<int?> { 5, 5, 5, 10, 10, 10 };
        var result = AutoregulationService.EvaluateSession(rpes, currentSessionSets: 15);

        result.Should().Contain("deload");
    }

    // --- Entity computed property ---

    [Fact]
    public void WorkoutSet_RirDrift_ComputesCorrectly()
    {
        var set = new Core.Entities.WorkoutSet { Rir = 1, TargetRir = 3 };
        set.RirDrift.Should().Be(-2);
    }

    [Fact]
    public void WorkoutSet_RirDrift_NullWhenMissing()
    {
        var set = new Core.Entities.WorkoutSet { Rir = null, TargetRir = 3 };
        set.RirDrift.Should().BeNull();

        var set2 = new Core.Entities.WorkoutSet { Rir = 2, TargetRir = null };
        set2.RirDrift.Should().BeNull();
    }
}
