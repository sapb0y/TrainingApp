using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Tests.Entities;

public class WorkoutSetTests
{
    [Fact]
    public void EstimatedOneRepMax_WithValidData_CalculatesEpleyFormula()
    {
        // Arrange: 100kg x 10 reps => 100 * (1 + 10/30) = 100 * 1.333... = 133.33
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = 10
        };

        // Act
        var e1rm = set.EstimatedOneRepMax;

        // Assert
        Assert.NotNull(e1rm);
        Assert.Equal(133.33m, Math.Round(e1rm.Value, 2));
    }

    [Fact]
    public void EstimatedOneRepMax_With1Rep_ReturnsWeight()
    {
        // Arrange: 1 rep = actual weight
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = 1
        };

        // Act
        var e1rm = set.EstimatedOneRepMax;

        // Assert
        Assert.NotNull(e1rm);
        Assert.Equal(103.33m, Math.Round(e1rm.Value, 2)); // 100 * (1 + 1/30)
    }

    [Fact]
    public void EstimatedOneRepMax_WithNullWeight_ReturnsNull()
    {
        var set = new WorkoutSet
        {
            ActualWeight = null,
            ActualReps = 10
        };

        Assert.Null(set.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_WithNullReps_ReturnsNull()
    {
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = null
        };

        Assert.Null(set.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_WithZeroReps_ReturnsNull()
    {
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = 0
        };

        Assert.Null(set.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_WithNegativeReps_ReturnsNull()
    {
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = -1
        };

        Assert.Null(set.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_WithOver30Reps_ReturnsNull()
    {
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = 31
        };

        Assert.Null(set.EstimatedOneRepMax);
    }

    [Fact]
    public void EstimatedOneRepMax_AtBoundary30Reps_ReturnsValue()
    {
        // 100 * (1 + 30/30) = 100 * 2 = 200
        var set = new WorkoutSet
        {
            ActualWeight = 100m,
            ActualReps = 30
        };

        Assert.NotNull(set.EstimatedOneRepMax);
        Assert.Equal(200m, set.EstimatedOneRepMax);
    }
}
