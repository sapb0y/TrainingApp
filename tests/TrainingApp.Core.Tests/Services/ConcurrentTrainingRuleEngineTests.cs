using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;
using static TrainingApp.Core.Services.FatigueRuleEngine;

namespace TrainingApp.Core.Tests.Services;

public class ConcurrentTrainingRuleEngineTests
{
    [Fact]
    public void HighInterference_FiresAbove7_WithBothSessions()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 7.5m, hasStrengthToday: true, hasCardioToday: true,
            separationHours: 4m, zone: CardioIntensityZone.Zone4,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string> { "chest" },
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 20);

        alerts.Should().Contain(a => a.RuleName == "High Interference");
        alerts.First(a => a.RuleName == "High Interference").Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void InsufficientSeparation_FiresBelow3Hr_Zone3Plus()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 5m, hasStrengthToday: true, hasCardioToday: true,
            separationHours: 2m, zone: CardioIntensityZone.Zone3,
            modality: CardioModality.Cycling, strengthMuscleGroups: new List<string> { "chest" },
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 20);

        alerts.Should().Contain(a => a.RuleName == "Insufficient Separation");
    }

    [Fact]
    public void ExcessiveCardioVolume_FiresWhenCardioOver2xStrength()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 3m, hasStrengthToday: false, hasCardioToday: true,
            separationHours: null, zone: CardioIntensityZone.Zone2,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string>(),
            weeklyCardioTrimp: 50m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 20);

        alerts.Should().Contain(a => a.RuleName == "Excessive Cardio Volume");
        alerts.First(a => a.RuleName == "Excessive Cardio Volume").Severity.Should().Be(AlertSeverity.Info);
    }

    [Fact]
    public void RunningPlusLegDay_Fires()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 5m, hasStrengthToday: true, hasCardioToday: true,
            separationHours: 4m, zone: CardioIntensityZone.Zone2,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string> { "quadriceps", "hamstrings" },
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 20);

        alerts.Should().Contain(a => a.RuleName == "Running + Leg Day");
    }

    [Fact]
    public void ZonePolarization_FiresAbove30Pct()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 3m, hasStrengthToday: false, hasCardioToday: true,
            separationHours: null, zone: CardioIntensityZone.Zone3,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string>(),
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 35);

        alerts.Should().Contain(a => a.RuleName == "Zone Polarization");
    }

    [Fact]
    public void NoAlerts_WhenNormal()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 3m, hasStrengthToday: true, hasCardioToday: true,
            separationHours: 5m, zone: CardioIntensityZone.Zone2,
            modality: CardioModality.Cycling, strengthMuscleGroups: new List<string> { "chest" },
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 15);

        alerts.Should().BeEmpty();
    }

    [Fact]
    public void MultipleAlerts_FireSimultaneously()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 8m, hasStrengthToday: true, hasCardioToday: true,
            separationHours: 1m, zone: CardioIntensityZone.Zone4,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string> { "quadriceps" },
            weeklyCardioTrimp: 50m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 40);

        alerts.Should().HaveCountGreaterThanOrEqualTo(4);
        alerts.Select(a => a.RuleName).Should().Contain("High Interference");
        alerts.Select(a => a.RuleName).Should().Contain("Insufficient Separation");
        alerts.Select(a => a.RuleName).Should().Contain("Running + Leg Day");
        alerts.Select(a => a.RuleName).Should().Contain("Zone Polarization");
    }

    [Fact]
    public void NoAlerts_WhenOnlyCardioNoStrength()
    {
        var alerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            interferenceScore: 8m, hasStrengthToday: false, hasCardioToday: true,
            separationHours: null, zone: CardioIntensityZone.Zone4,
            modality: CardioModality.Running, strengthMuscleGroups: new List<string>(),
            weeklyCardioTrimp: 10m, weeklyStrengthTrimp: 20m, weeklyZone3PlusPct: 20);

        // High interference and insufficient separation require both sessions
        alerts.Should().NotContain(a => a.RuleName == "High Interference");
        alerts.Should().NotContain(a => a.RuleName == "Running + Leg Day");
    }
}
