using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class LoadPrescriptionServiceTests
{
    [Theory]
    [InlineData(ProgramGoal.Strength, 3, 6)]
    [InlineData(ProgramGoal.Hypertrophy, 8, 12)]
    [InlineData(ProgramGoal.PowerBuilding, 5, 8)]
    [InlineData(ProgramGoal.GeneralFitness, 8, 15)]
    public void GetRepRange_ReturnsCorrectRange(ProgramGoal goal, int expectedMin, int expectedMax)
    {
        var (min, max) = LoadPrescriptionService.GetRepRange(goal);

        min.Should().Be(expectedMin);
        max.Should().Be(expectedMax);
    }

    [Theory]
    [InlineData(1, 96.8)]
    [InlineData(5, 85.7)]
    [InlineData(10, 75.0)]
    [InlineData(12, 71.4)]
    public void EstimatePercentageForReps_ReturnsCorrectPercentage(int reps, decimal expected)
    {
        var result = LoadPrescriptionService.EstimatePercentageForReps(reps);
        result.Should().Be(expected);
    }

    [Fact]
    public void EstimatePercentageForReps_ThrowsForZeroReps()
    {
        var act = () => LoadPrescriptionService.EstimatePercentageForReps(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateTargetWeight_CalculatesCorrectly()
    {
        // 100kg e1RM, 10 reps → 75% → 75kg
        var result = LoadPrescriptionService.CalculateTargetWeight(100m, 10);
        result.Should().Be(75.0m);
    }

    [Fact]
    public void CalculateTargetWeight_ThrowsForZeroE1rm()
    {
        var act = () => LoadPrescriptionService.CalculateTargetWeight(0m, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(82.3, 2.5, 82.5)]
    [InlineData(81.2, 2.5, 80.0)]
    [InlineData(83.7, 2.5, 82.5)]
    [InlineData(10.5, 1.25, 10.0)]
    [InlineData(11.0, 1.25, 11.25)]
    public void RoundToIncrement_RoundsCorrectly(decimal weight, decimal increment, decimal expected)
    {
        var result = LoadPrescriptionService.RoundToIncrement(weight, increment);
        result.Should().Be(expected);
    }

    [Fact]
    public void RoundToIncrement_ThrowsForZeroIncrement()
    {
        var act = () => LoadPrescriptionService.RoundToIncrement(100m, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
