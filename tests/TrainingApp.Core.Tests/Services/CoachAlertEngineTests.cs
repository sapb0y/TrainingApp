using FluentAssertions;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class CoachAlertEngineTests
{
    private static readonly Guid AthleteId = Guid.NewGuid();
    private const string AthleteName = "Test Athlete";

    [Fact]
    public void EvaluateFatigue_TsbBelowNeg10_ReturnsWarning()
    {
        var alert = CoachAlertEngine.EvaluateFatigue(AthleteId, AthleteName, -15m, null);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Warning");
        alert.Category.Should().Be(CoachAlertEngine.CoachAlertCategory.FatigueRisk);
    }

    [Fact]
    public void EvaluateFatigue_ReadinessBelow50_ReturnsWarning()
    {
        var alert = CoachAlertEngine.EvaluateFatigue(AthleteId, AthleteName, 0m, 40m);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Warning");
    }

    [Fact]
    public void EvaluateFatigue_HealthyValues_ReturnsNull()
    {
        var alert = CoachAlertEngine.EvaluateFatigue(AthleteId, AthleteName, 5m, 75m);
        alert.Should().BeNull();
    }

    [Fact]
    public void EvaluateOverreaching_AvgRpeAbove9_ReturnsWarning()
    {
        var alert = CoachAlertEngine.EvaluateOverreaching(AthleteId, AthleteName, 9.5m);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Warning");
    }

    [Fact]
    public void EvaluateOverreaching_NormalRpe_ReturnsNull()
    {
        var alert = CoachAlertEngine.EvaluateOverreaching(AthleteId, AthleteName, 7.5m);
        alert.Should().BeNull();
    }

    [Fact]
    public void EvaluateMissedSessions_MoreThan2_ReturnsInfo()
    {
        var alert = CoachAlertEngine.EvaluateMissedSessions(AthleteId, AthleteName, 3);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Info");
    }

    [Fact]
    public void EvaluateRpeDrift_DeviationAbove1_5_ReturnsWarning()
    {
        var alert = CoachAlertEngine.EvaluateRpeDrift(AthleteId, AthleteName, 2.0m);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Warning");
    }

    [Fact]
    public void EvaluateDeficitStress_InDeficitWithDecline_ReturnsAlert()
    {
        var alert = CoachAlertEngine.EvaluateDeficitStress(AthleteId, AthleteName, true, -7.5m);
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be("Alert");
        alert.Category.Should().Be(CoachAlertEngine.CoachAlertCategory.DeficitStress);
    }

    [Fact]
    public void SortBySeverity_AlertsFirst_ThenWarning_ThenInfo()
    {
        var alerts = new List<CoachAlertEngine.CoachAlert>
        {
            new(AthleteId, "A", CoachAlertEngine.CoachAlertCategory.MissedSessions, "Info", "msg", null),
            new(AthleteId, "A", CoachAlertEngine.CoachAlertCategory.DeficitStress, "Alert", "msg", null),
            new(AthleteId, "A", CoachAlertEngine.CoachAlertCategory.FatigueRisk, "Warning", "msg", null),
        };

        var sorted = CoachAlertEngine.SortBySeverity(alerts);
        sorted[0].Severity.Should().Be("Alert");
        sorted[1].Severity.Should().Be("Warning");
        sorted[2].Severity.Should().Be("Info");
    }
}
