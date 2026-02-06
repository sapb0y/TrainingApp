using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class MesocycleCalculatorServiceTests
{
    [Theory]
    [InlineData(TrainingExperience.Beginner, 25, RecoveryCapacity.Normal, 8)]
    [InlineData(TrainingExperience.Intermediate, 35, RecoveryCapacity.Normal, 5)]
    [InlineData(TrainingExperience.Advanced, 35, RecoveryCapacity.Normal, 4)]
    [InlineData(TrainingExperience.Advanced, 50, RecoveryCapacity.Low, 3)]  // 4-1-1=2 → clamped to 3
    [InlineData(TrainingExperience.Beginner, 25, RecoveryCapacity.High, 9)]  // 8+0+1=9
    [InlineData(TrainingExperience.Advanced, 65, RecoveryCapacity.Low, 3)]  // 4-2-1=1 → clamped to 3
    [InlineData(TrainingExperience.Beginner, 20, RecoveryCapacity.High, 9)]  // 8+0+1=9
    [InlineData(TrainingExperience.Beginner, null, RecoveryCapacity.Normal, 8)]  // null age → 0 mod
    public void CalculateAccumulationWeeks_ReturnsCorrectWeeks(
        TrainingExperience exp, int? age, RecoveryCapacity recovery, int expected)
    {
        var result = MesocycleCalculatorService.CalculateAccumulationWeeks(exp, age, recovery);
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateAccumulationWeeks_NeverBelowMinimum()
    {
        // Most extreme: Advanced + old + low recovery
        var result = MesocycleCalculatorService.CalculateAccumulationWeeks(
            TrainingExperience.Advanced, 70, RecoveryCapacity.Low);
        result.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void CalculateAccumulationWeeks_NeverAboveMaximum()
    {
        // Most favorable: Beginner + young + high recovery
        var result = MesocycleCalculatorService.CalculateAccumulationWeeks(
            TrainingExperience.Beginner, 20, RecoveryCapacity.High);
        result.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void GeneratePhaseStructure_8WeekWith4Accum()
    {
        // 4+1+3 = 8
        var phases = MesocycleCalculatorService.GeneratePhaseStructure(8, 4);

        phases.Should().HaveCount(3);
        phases[0].Should().Be((PhaseType.Accumulation, 4));
        phases[1].Should().Be((PhaseType.Deload, 1));
        phases[2].Should().Be((PhaseType.Accumulation, 3));
    }

    [Fact]
    public void GeneratePhaseStructure_12WeekWith5Accum()
    {
        // 5+1+5+1 = 12
        var phases = MesocycleCalculatorService.GeneratePhaseStructure(12, 5);

        phases.Should().HaveCount(4);
        phases[0].Should().Be((PhaseType.Accumulation, 5));
        phases[1].Should().Be((PhaseType.Deload, 1));
        phases[2].Should().Be((PhaseType.Accumulation, 5));
        phases[3].Should().Be((PhaseType.Deload, 1));
    }

    [Fact]
    public void GeneratePhaseStructure_10WeekWith4Accum()
    {
        // 4+1+4+1 = 10
        var phases = MesocycleCalculatorService.GeneratePhaseStructure(10, 4);

        phases.Should().HaveCount(4);
        phases[0].Should().Be((PhaseType.Accumulation, 4));
        phases[1].Should().Be((PhaseType.Deload, 1));
        phases[2].Should().Be((PhaseType.Accumulation, 4));
        phases[3].Should().Be((PhaseType.Deload, 1));
    }

    [Fact]
    public void GeneratePhaseStructure_16WeekWith5Accum()
    {
        // 5+1+5+1+4 = 16
        var phases = MesocycleCalculatorService.GeneratePhaseStructure(16, 5);

        phases.Should().HaveCount(5);
        phases[0].Should().Be((PhaseType.Accumulation, 5));
        phases[1].Should().Be((PhaseType.Deload, 1));
        phases[2].Should().Be((PhaseType.Accumulation, 5));
        phases[3].Should().Be((PhaseType.Deload, 1));
        phases[4].Should().Be((PhaseType.Accumulation, 4));
    }

    [Fact]
    public void GeneratePhaseStructure_ThrowsForZeroWeeks()
    {
        var act = () => MesocycleCalculatorService.GeneratePhaseStructure(0, 4);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GeneratePhaseStructure_TotalWeeksMatch()
    {
        var phases = MesocycleCalculatorService.GeneratePhaseStructure(12, 5);
        phases.Sum(p => p.Weeks).Should().Be(12);
    }
}
