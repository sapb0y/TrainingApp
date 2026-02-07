using FluentAssertions;
using TrainingApp.Core.Services;
using static TrainingApp.Core.Services.FatigueRuleEngine;

namespace TrainingApp.Core.Tests.Services;

public class FatLossRuleEngineTests
{
    [Fact]
    public void EvaluateRules_RapidWeightLoss_FiresAtNeg1_1()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -1.1m, weeksInDeficit: 4, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().Contain(a => a.RuleName == "Rapid Weight Loss");
        alerts.First(a => a.RuleName == "Rapid Weight Loss").Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void EvaluateRules_NoRapidWeightLoss_AtNeg1_0()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -1.0m, weeksInDeficit: 4, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().NotContain(a => a.RuleName == "Rapid Weight Loss");
    }

    [Fact]
    public void EvaluateRules_StalledWeightLoss_WhenRateAboveNeg0_1AfterTwoWeeks()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.05m, weeksInDeficit: 3, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().Contain(a => a.RuleName == "Stalled Weight Loss");
        alerts.First(a => a.RuleName == "Stalled Weight Loss").Severity.Should().Be(AlertSeverity.Info);
    }

    [Fact]
    public void EvaluateRules_NoStall_WhenUnderTwoWeeks()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.05m, weeksInDeficit: 2, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().NotContain(a => a.RuleName == "Stalled Weight Loss");
    }

    [Fact]
    public void EvaluateRules_MetabolicAdaptationHigh_FiresAbove15Percent()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.5m, weeksInDeficit: 10, isDeficitActive: true,
            adaptationPercent: 16m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().Contain(a => a.RuleName == "Metabolic Adaptation High");
        alerts.First(a => a.RuleName == "Metabolic Adaptation High").Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void EvaluateRules_NeatDecline_FiresAbove20Percent()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.5m, weeksInDeficit: 4, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 25m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().Contain(a => a.RuleName == "NEAT Decline");
    }

    [Fact]
    public void EvaluateRules_DietBreakDue_FiresWhenOverdue()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.5m, weeksInDeficit: 8, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 50, breakIntervalDays: 42);

        alerts.Should().Contain(a => a.RuleName == "Diet Break Due");
        alerts.First(a => a.RuleName == "Diet Break Due").Severity.Should().Be(AlertSeverity.Info);
    }

    [Fact]
    public void EvaluateRules_NoAlerts_WhenNormalValues()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.5m, weeksInDeficit: 4, isDeficitActive: true,
            adaptationPercent: 5m, neatCompensationPercent: 10m, daysSinceBreak: 14, breakIntervalDays: 42);

        alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateRules_MultipleAlerts_CanFireSimultaneously()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -1.5m, weeksInDeficit: 12, isDeficitActive: true,
            adaptationPercent: 18m, neatCompensationPercent: 25m, daysSinceBreak: 50, breakIntervalDays: 42);

        alerts.Should().HaveCountGreaterThanOrEqualTo(4);
        alerts.Select(a => a.RuleName).Should().Contain("Rapid Weight Loss");
        alerts.Select(a => a.RuleName).Should().Contain("Metabolic Adaptation High");
        alerts.Select(a => a.RuleName).Should().Contain("NEAT Decline");
        alerts.Select(a => a.RuleName).Should().Contain("Diet Break Due");
    }

    [Fact]
    public void EvaluateRules_NoAlerts_WhenDeficitNotActive()
    {
        var alerts = FatLossRuleEngine.EvaluateRules(
            weeklyRateKg: -0.05m, weeksInDeficit: 10, isDeficitActive: false,
            adaptationPercent: 18m, neatCompensationPercent: 10m, daysSinceBreak: 50, breakIntervalDays: 42);

        // Stalled, adaptation, diet break all gate on isDeficitActive
        alerts.Should().NotContain(a => a.RuleName == "Stalled Weight Loss");
        alerts.Should().NotContain(a => a.RuleName == "Metabolic Adaptation High");
        alerts.Should().NotContain(a => a.RuleName == "Diet Break Due");
    }
}
