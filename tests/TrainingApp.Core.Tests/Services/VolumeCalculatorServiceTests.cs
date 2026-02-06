using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class VolumeCalculatorServiceTests
{
    [Theory]
    [InlineData("Chest", 8, 14, 20)]
    [InlineData("Back", 8, 14, 20)]
    [InlineData("Shoulders", 6, 12, 18)]
    [InlineData("Quads", 6, 12, 18)]
    [InlineData("Hamstrings", 4, 10, 16)]
    [InlineData("Glutes", 4, 10, 16)]
    [InlineData("Biceps", 4, 10, 16)]
    [InlineData("Triceps", 4, 10, 14)]
    [InlineData("Calves", 4, 8, 14)]
    [InlineData("Abs", 0, 8, 16)]
    [InlineData("Traps", 0, 8, 14)]
    [InlineData("Forearms", 0, 6, 12)]
    public void GetVolumeLandmarks_ReturnsCorrectValues(string muscle, int mev, int mav, int mrv)
    {
        var result = VolumeCalculatorService.GetVolumeLandmarks(muscle);

        result.Mev.Should().Be(mev);
        result.Mav.Should().Be(mav);
        result.Mrv.Should().Be(mrv);
    }

    [Fact]
    public void GetVolumeLandmarks_ThrowsForUnknownMuscle()
    {
        var act = () => VolumeCalculatorService.GetVolumeLandmarks("Unknown");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Chest", ProgramGoal.Hypertrophy, 14)]
    [InlineData("Chest", ProgramGoal.Strength, 11)]
    [InlineData("Back", ProgramGoal.Hypertrophy, 14)]
    [InlineData("Abs", ProgramGoal.Hypertrophy, 8)]
    [InlineData("Abs", ProgramGoal.Strength, 4)]
    [InlineData("Triceps", ProgramGoal.GeneralFitness, 7)]
    public void CalculateWeeklyVolume_ReturnsGoalAppropriateVolume(string muscle, ProgramGoal goal, int expected)
    {
        var result = VolumeCalculatorService.CalculateWeeklyVolume(muscle, goal);
        result.Should().Be(expected);
    }

    [Fact]
    public void AllMuscleGroups_Returns12Groups()
    {
        VolumeCalculatorService.AllMuscleGroups.Should().HaveCount(12);
    }
}
