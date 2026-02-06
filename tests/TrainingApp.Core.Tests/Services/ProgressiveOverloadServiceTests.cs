using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class ProgressiveOverloadServiceTests
{
    [Fact]
    public void CalculateProgression_IncreasesWeight_WhenWellAboveTargetRir()
    {
        // actualRir (5) > targetRir (2) + 1
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 80m, prevReps: 10, targetRir: 2, actualRir: 5, ProgramGoal.Hypertrophy);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.WeightIncrease);
        result.Weight.Should().Be(82.5m);
        result.Reps.Should().Be(8); // reset to repMin for Hypertrophy
    }

    [Fact]
    public void CalculateProgression_IncreasesReps_WhenAtTargetRir()
    {
        // actualRir (2) == targetRir (2), reps < max
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 80m, prevReps: 10, targetRir: 2, actualRir: 2, ProgramGoal.Hypertrophy);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.RepIncrease);
        result.Weight.Should().Be(80m);
        result.Reps.Should().Be(11);
    }

    [Fact]
    public void CalculateProgression_IncreasesWeight_WhenRepsMaxedAtTargetRir()
    {
        // actualRir (2) == targetRir (2), reps at max (12 for Hypertrophy)
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 80m, prevReps: 12, targetRir: 2, actualRir: 2, ProgramGoal.Hypertrophy);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.WeightIncrease);
        result.Weight.Should().Be(82.5m);
        result.Reps.Should().Be(8); // reset to repMin
    }

    [Fact]
    public void CalculateProgression_Maintains_WhenStruggling()
    {
        // actualRir (1) < targetRir (2)
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 80m, prevReps: 10, targetRir: 2, actualRir: 1, ProgramGoal.Hypertrophy);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.Maintain);
        result.Weight.Should().Be(80m);
        result.Reps.Should().Be(10);
    }

    [Fact]
    public void CalculateProgression_StrengthGoal_UsesCorrectRepRange()
    {
        // Strength: 3-6 reps. At target RIR with reps at max (6) → weight increase
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 140m, prevReps: 6, targetRir: 2, actualRir: 2, ProgramGoal.Strength);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.WeightIncrease);
        result.Weight.Should().Be(142.5m);
        result.Reps.Should().Be(3); // reset to repMin for Strength
    }

    [Fact]
    public void CalculateProgression_IncreasesReps_WhenSlightlyAboveTarget()
    {
        // actualRir (3) == targetRir (2) + 1 → not "well above", just at target range
        var result = ProgressiveOverloadService.CalculateProgression(
            prevWeight: 80m, prevReps: 9, targetRir: 2, actualRir: 3, ProgramGoal.Hypertrophy);

        result.Type.Should().Be(ProgressiveOverloadService.ProgressionType.RepIncrease);
        result.Weight.Should().Be(80m);
        result.Reps.Should().Be(10);
    }

    [Theory]
    [InlineData("Biceps", 1.25)]
    [InlineData("Triceps", 1.25)]
    [InlineData("Forearms", 1.25)]
    [InlineData("Calves", 1.25)]
    [InlineData("Abs", 1.25)]
    [InlineData("Chest", 2.5)]
    [InlineData("Back", 2.5)]
    [InlineData("Quads", 2.5)]
    [InlineData("default", 2.5)]
    public void GetMinWeightIncrement_ReturnsCorrectIncrement(string muscle, decimal expected)
    {
        var result = ProgressiveOverloadService.GetMinWeightIncrement(muscle);
        result.Should().Be(expected);
    }
}
