using FluentAssertions;
using TrainingApp.Core.Services;
using static TrainingApp.Core.Services.PartnerSchedulingService;

namespace TrainingApp.Core.Tests.Services;

public class PartnerSchedulingServiceTests
{
    private static ExercisePlan MakePlan(string name, List<string> equipment, string category = "Chest",
        int sets = 3, int rest = 90, int order = 0)
        => new(Guid.NewGuid(), Guid.NewGuid(), name, equipment, category, sets,
            EstimateSetDuration(category), rest, order);

    [Fact]
    public void GenerateSchedule_BasicInterleave_ProducesBlocks()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench Press", ["Barbell", "Flat Bench"], order: 0) };
        var planB = new List<ExercisePlan> { MakePlan("Squat", ["Barbell", "Squat Rack"], order: 0) };

        var result = GenerateSchedule(planA, planB);

        result.Blocks.Should().NotBeEmpty();
        result.Summary.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSchedule_NoEquipmentConflict_ParallelBlocks()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench Press", ["Flat Bench"]) };
        var planB = new List<ExercisePlan> { MakePlan("Squat", ["Squat Rack"]) };

        var result = GenerateSchedule(planA, planB);

        // Should have at least some parallel work blocks
        result.Blocks.Should().Contain(b =>
            b.UserA.Type == ActionType.Work && b.UserB.Type == ActionType.Work);
        result.Summary.EquipmentConflicts.Should().Be(0);
    }

    [Fact]
    public void GenerateSchedule_EquipmentConflict_SequentialBlocks()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench Press", ["Barbell"]) };
        var planB = new List<ExercisePlan> { MakePlan("Barbell Row", ["Barbell"]) };

        var result = GenerateSchedule(planA, planB);

        result.Summary.EquipmentConflicts.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSchedule_AllSameEquipment_HasConflicts()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench", ["Barbell"], sets: 2) };
        var planB = new List<ExercisePlan> { MakePlan("OHP", ["Barbell"], sets: 2) };

        var result = GenerateSchedule(planA, planB);

        result.Summary.EquipmentConflicts.Should().BeGreaterThanOrEqualTo(1);
        // With same equipment, some blocks must be sequential
        result.Blocks.Should().Contain(b =>
            b.UserA.Type == ActionType.Work && b.UserB.Type == ActionType.Rest);
    }

    [Fact]
    public void GenerateSchedule_AsymmetricVolumes_CompletesAll()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench", ["Flat Bench"], sets: 5) };
        var planB = new List<ExercisePlan> { MakePlan("Curls", ["Dumbbells"], category: "Arms", sets: 2) };

        var result = GenerateSchedule(planA, planB);

        // All sets from both should be represented
        var aWorkBlocks = result.Blocks.Count(b => b.UserA.Type == ActionType.Work);
        var bWorkBlocks = result.Blocks.Count(b => b.UserB.Type == ActionType.Work);
        aWorkBlocks.Should().Be(5);
        bWorkBlocks.Should().Be(2);
    }

    [Fact]
    public void GenerateSchedule_EmptyPlanA_OnlyBWorks()
    {
        var planA = new List<ExercisePlan>();
        var planB = new List<ExercisePlan> { MakePlan("Squat", ["Squat Rack"], sets: 3) };

        var result = GenerateSchedule(planA, planB);

        result.Blocks.Should().NotBeEmpty();
        result.Blocks.Should().OnlyContain(b => b.UserA.Type != ActionType.Work);
        result.Blocks.Where(b => b.UserB.Type == ActionType.Work).Should().HaveCount(3);
    }

    [Fact]
    public void GenerateSchedule_EmptyPlanB_OnlyAWorks()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench", ["Barbell"], sets: 2) };
        var planB = new List<ExercisePlan>();

        var result = GenerateSchedule(planA, planB);

        result.Blocks.Should().NotBeEmpty();
        result.Blocks.Should().OnlyContain(b => b.UserB.Type != ActionType.Work);
        result.Blocks.Where(b => b.UserA.Type == ActionType.Work).Should().HaveCount(2);
    }

    [Fact]
    public void GenerateSchedule_BothEmpty_NoBlocks()
    {
        var result = GenerateSchedule([], []);

        result.Blocks.Should().BeEmpty();
        result.Summary.TotalSeconds.Should().Be(0);
    }

    [Fact]
    public void GenerateSchedule_PreservesExerciseOrder()
    {
        var planA = new List<ExercisePlan>
        {
            MakePlan("Bench", ["Flat Bench"], sets: 1, order: 0),
            MakePlan("Flyes", ["Cables"], sets: 1, order: 1)
        };
        var planB = new List<ExercisePlan>();

        var result = GenerateSchedule(planA, planB);

        var workBlocks = result.Blocks.Where(b => b.UserA.Type == ActionType.Work).ToList();
        workBlocks.Should().HaveCount(2);
        workBlocks[0].UserA.ExerciseName.Should().Be("Bench");
        workBlocks[1].UserA.ExerciseName.Should().Be("Flyes");
    }

    [Fact]
    public void GenerateSchedule_TimeSaved_GreaterThanZero()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench", ["Flat Bench"], sets: 3) };
        var planB = new List<ExercisePlan> { MakePlan("Squat", ["Squat Rack"], sets: 3) };

        var result = GenerateSchedule(planA, planB);

        result.Summary.TimeSavedSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSchedule_Utilization_BothAboveZero()
    {
        var planA = new List<ExercisePlan> { MakePlan("Bench", ["Flat Bench"], sets: 3) };
        var planB = new List<ExercisePlan> { MakePlan("Squat", ["Squat Rack"], sets: 3) };

        var result = GenerateSchedule(planA, planB);

        result.Summary.UtilizationPercentA.Should().BeGreaterThan(0);
        result.Summary.UtilizationPercentB.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HasEquipmentConflict_Overlap_ReturnsTrue()
    {
        HasEquipmentConflict(["Barbell"], ["Barbell"]).Should().BeTrue();
    }

    [Fact]
    public void HasEquipmentConflict_NoOverlap_ReturnsFalse()
    {
        HasEquipmentConflict(["Barbell"], ["Dumbbells"]).Should().BeFalse();
    }

    [Fact]
    public void HasEquipmentConflict_Empty_ReturnsFalse()
    {
        HasEquipmentConflict([], ["Barbell"]).Should().BeFalse();
        HasEquipmentConflict(["Barbell"], []).Should().BeFalse();
        HasEquipmentConflict([], []).Should().BeFalse();
    }

    [Fact]
    public void HasEquipmentConflict_CaseInsensitive_ReturnsTrue()
    {
        HasEquipmentConflict(["barbell"], ["BARBELL"]).Should().BeTrue();
    }

    [Fact]
    public void EstimateSoloDuration_CalculatesCorrectly()
    {
        var plan = new List<ExercisePlan>
        {
            MakePlan("Bench", ["Barbell"], sets: 3, rest: 90)
        };

        var result = EstimateSoloDuration(plan);

        // 3 sets * (45s work + 90s rest) = 405
        result.Should().Be(405);
    }

    [Fact]
    public void EstimateSetDuration_Compound_45s()
    {
        EstimateSetDuration("Chest").Should().Be(45);
        EstimateSetDuration("Back").Should().Be(45);
        EstimateSetDuration("Legs").Should().Be(45);
        EstimateSetDuration("Shoulders").Should().Be(45);
    }

    [Fact]
    public void EstimateSetDuration_Isolation_30s()
    {
        EstimateSetDuration("Arms").Should().Be(30);
        EstimateSetDuration("Biceps").Should().Be(30);
        EstimateSetDuration("Abs").Should().Be(30);
    }

    [Fact]
    public void EstimateSetDuration_Unknown_40s()
    {
        EstimateSetDuration("Other").Should().Be(40);
        EstimateSetDuration("Stretching").Should().Be(40);
    }
}
