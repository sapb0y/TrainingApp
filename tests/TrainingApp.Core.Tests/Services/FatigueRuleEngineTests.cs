using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class FatigueRuleEngineTests
{
    [Fact]
    public void EvaluateRules_DeloadSuggestion_WhenTsbBelow20()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: -25m, readinessScore: 5m, daysSinceDeload: 14, daysSinceLastWorkout: 1);
        alerts.Should().ContainSingle(a => a.RuleName == "TSB Deload Suggestion");
        alerts.First(a => a.RuleName == "TSB Deload Suggestion").Severity.Should().Be(FatigueRuleEngine.AlertSeverity.Warning);
    }

    [Fact]
    public void EvaluateRules_OverreachingWarning_WhenTsbBelow30()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: -35m, readinessScore: 5m, daysSinceDeload: 14, daysSinceLastWorkout: 1);
        alerts.Should().ContainSingle(a => a.RuleName == "TSB Overreaching Warning");
        alerts.First(a => a.RuleName == "TSB Overreaching Warning").Severity.Should().Be(FatigueRuleEngine.AlertSeverity.Alert);
        // Should NOT also fire deload suggestion (mutually exclusive)
        alerts.Should().NotContain(a => a.RuleName == "TSB Deload Suggestion");
    }

    [Fact]
    public void EvaluateRules_DeloadOverdue_WhenOver28Days()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: 0m, readinessScore: 5m, daysSinceDeload: 35, daysSinceLastWorkout: 1);
        alerts.Should().ContainSingle(a => a.RuleName == "Deload Overdue");
        alerts.First(a => a.RuleName == "Deload Overdue").Message.Should().Contain("35");
    }

    [Fact]
    public void EvaluateRules_PoorRecovery_WhenReadinessBelow3()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: 0m, readinessScore: 2.5m, daysSinceDeload: 14, daysSinceLastWorkout: 1);
        alerts.Should().ContainSingle(a => a.RuleName == "Poor Recovery");
        alerts.First(a => a.RuleName == "Poor Recovery").Scope.Should().Be("PreWorkout");
    }

    [Fact]
    public void EvaluateRules_DetrainingRisk_WhenHighTsbAndInactive()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: 20m, readinessScore: 8m, daysSinceDeload: 14, daysSinceLastWorkout: 10);
        alerts.Should().ContainSingle(a => a.RuleName == "Detraining Risk");
        alerts.First(a => a.RuleName == "Detraining Risk").Severity.Should().Be(FatigueRuleEngine.AlertSeverity.Info);
    }

    [Fact]
    public void EvaluateRules_NoAlerts_WhenNormalValues()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: 5m, readinessScore: 6m, daysSinceDeload: 14, daysSinceLastWorkout: 1);
        alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateRules_MultipleAlerts_WhenMultipleConditions()
    {
        // TSB < -20 + deload overdue + poor recovery
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: -25m, readinessScore: 2m, daysSinceDeload: 35, daysSinceLastWorkout: 1);
        alerts.Should().HaveCount(3);
        alerts.Select(a => a.RuleName).Should().Contain("TSB Deload Suggestion");
        alerts.Select(a => a.RuleName).Should().Contain("Deload Overdue");
        alerts.Select(a => a.RuleName).Should().Contain("Poor Recovery");
    }

    [Fact]
    public void EvaluateRules_NullReadiness_DoesNotFirePoorRecovery()
    {
        var alerts = FatigueRuleEngine.EvaluateRules(tsb: 0m, readinessScore: null, daysSinceDeload: 14, daysSinceLastWorkout: 1);
        alerts.Should().NotContain(a => a.RuleName == "Poor Recovery");
    }
}
