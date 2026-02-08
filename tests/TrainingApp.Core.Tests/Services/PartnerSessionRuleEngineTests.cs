using FluentAssertions;
using TrainingApp.Core.Services;
using static TrainingApp.Core.Services.PartnerSchedulingService;

namespace TrainingApp.Core.Tests.Services;

public class PartnerSessionRuleEngineTests
{
    private static PartnerSchedule MakeSchedule(int blocks = 10, int conflicts = 0,
        decimal utilA = 60, decimal utilB = 60)
    {
        var blockList = Enumerable.Range(1, blocks).Select(i =>
            new ScheduleBlock(i,
                new SlotAction(null, null, null, ActionType.Work, null),
                new SlotAction(null, null, null, ActionType.Work, null),
                45)).ToList();

        var summary = new ScheduleSummary(blocks * 45, blocks * 45, blocks * 45,
            0, conflicts, utilA, utilB, []);

        return new PartnerSchedule(blockList, summary);
    }

    [Fact]
    public void FatigueMismatch_WhenTsbDiffOver20_ReturnsWarning()
    {
        var schedule = MakeSchedule();
        var alerts = PartnerSessionRuleEngine.EvaluateRules(10m, -15m, 5, 5, schedule);

        alerts.Should().Contain(a => a.RuleName == "Fatigue Mismatch");
    }

    [Fact]
    public void FatigueMismatch_WhenTsbClose_NoAlert()
    {
        var schedule = MakeSchedule();
        var alerts = PartnerSessionRuleEngine.EvaluateRules(5m, 0m, 5, 5, schedule);

        alerts.Should().NotContain(a => a.RuleName == "Fatigue Mismatch");
    }

    [Fact]
    public void VolumeImbalance_WhenOneTwiceOther_ReturnsInfo()
    {
        var schedule = MakeSchedule();
        var alerts = PartnerSessionRuleEngine.EvaluateRules(0m, 0m, 8, 3, schedule);

        alerts.Should().Contain(a => a.RuleName == "Volume Imbalance");
    }

    [Fact]
    public void HighEquipmentConflicts_Over30Pct_ReturnsInfo()
    {
        var schedule = MakeSchedule(blocks: 10, conflicts: 4);
        var alerts = PartnerSessionRuleEngine.EvaluateRules(0m, 0m, 5, 5, schedule);

        alerts.Should().Contain(a => a.RuleName == "High Equipment Conflicts");
    }

    [Fact]
    public void LowUtilization_Under50Pct_ReturnsInfo()
    {
        var schedule = MakeSchedule(utilA: 30, utilB: 70);
        var alerts = PartnerSessionRuleEngine.EvaluateRules(0m, 0m, 5, 5, schedule);

        alerts.Should().Contain(a => a.RuleName == "Low Partner Utilization");
    }

    [Fact]
    public void MultipleAlerts_SimultaneousFiring()
    {
        var schedule = MakeSchedule(blocks: 10, conflicts: 5, utilA: 40, utilB: 40);
        var alerts = PartnerSessionRuleEngine.EvaluateRules(30m, -5m, 10, 3, schedule);

        alerts.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void NoAlerts_WhenHealthy()
    {
        var schedule = MakeSchedule(blocks: 10, conflicts: 0, utilA: 60, utilB: 60);
        var alerts = PartnerSessionRuleEngine.EvaluateRules(5m, 3m, 5, 5, schedule);

        alerts.Should().BeEmpty();
    }
}
