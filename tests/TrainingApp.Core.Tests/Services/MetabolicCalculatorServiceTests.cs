using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Tests.Services;

public class MetabolicCalculatorServiceTests
{
    // --- CalculateBmr ---

    [Fact]
    public void CalculateBmr_Male_ReturnsCorrectValue()
    {
        // 10*80 + 6.25*180 - 5*30 + 5 = 800 + 1125 - 150 + 5 = 1780
        var result = MetabolicCalculatorService.CalculateBmr(BiologicalSex.Male, 80m, 180m, 30);
        result.BmrKcal.Should().Be(1780.0m);
        result.Formula.Should().Be("Mifflin-St Jeor");
    }

    [Fact]
    public void CalculateBmr_Female_ReturnsCorrectValue()
    {
        // 10*65 + 6.25*165 - 5*28 - 161 = 650 + 1031.25 - 140 - 161 = 1380.25 → rounds to 1380.2
        var result = MetabolicCalculatorService.CalculateBmr(BiologicalSex.Female, 65m, 165m, 28);
        result.BmrKcal.Should().Be(1380.2m);
    }

    [Fact]
    public void CalculateBmr_ClampsToMinimum800()
    {
        // Very low values
        var result = MetabolicCalculatorService.CalculateBmr(BiologicalSex.Female, 30m, 100m, 80);
        result.BmrKcal.Should().Be(800m);
    }

    [Fact]
    public void CalculateBmr_ClampsToMaximum5000()
    {
        // 10*300 + 6.25*250 - 5*20 + 5 = 3000 + 1562.5 - 100 + 5 = 4467.5 (under 5000)
        var result = MetabolicCalculatorService.CalculateBmr(BiologicalSex.Male, 300m, 250m, 20);
        result.BmrKcal.Should().Be(4467.5m);
    }

    [Fact]
    public void CalculateBmr_HighWeight_StillUnder5000()
    {
        var result = MetabolicCalculatorService.CalculateBmr(BiologicalSex.Male, 500m, 200m, 20);
        result.BmrKcal.Should().Be(5000m); // would be 6255, clamped to 5000
    }

    // --- CalculateTdee ---

    [Fact]
    public void CalculateTdee_Sedentary_Multiplies1_2()
    {
        var result = MetabolicCalculatorService.CalculateTdee(1800m, ActivityLevel.Sedentary);
        result.TdeeKcal.Should().Be(2160.0m);
        result.ActivityMultiplier.Should().Be(1.2m);
    }

    [Fact]
    public void CalculateTdee_Moderate_Multiplies1_55()
    {
        var result = MetabolicCalculatorService.CalculateTdee(1800m, ActivityLevel.Moderate);
        result.TdeeKcal.Should().Be(2790.0m);
        result.ActivityMultiplier.Should().Be(1.55m);
    }

    [Fact]
    public void CalculateTdee_VeryActive_Multiplies1_9()
    {
        var result = MetabolicCalculatorService.CalculateTdee(2000m, ActivityLevel.VeryActive);
        result.TdeeKcal.Should().Be(3800.0m);
        result.ActivityMultiplier.Should().Be(1.9m);
    }

    [Fact]
    public void CalculateTdee_PreservesBmrInResult()
    {
        var result = MetabolicCalculatorService.CalculateTdee(1500m, ActivityLevel.Light);
        result.BmrKcal.Should().Be(1500m);
    }

    // --- EstimateAdaptation ---

    [Fact]
    public void EstimateAdaptation_NoWeightLoss_ReturnsZero()
    {
        var result = MetabolicCalculatorService.EstimateAdaptation(2500m, 80m, 80m, 4);
        result.AdaptationKcal.Should().Be(0m);
        result.AdaptationPercent.Should().Be(0m);
        result.AdaptedTdeeKcal.Should().Be(2500m);
    }

    [Fact]
    public void EstimateAdaptation_WeightGain_ReturnsZero()
    {
        var result = MetabolicCalculatorService.EstimateAdaptation(2500m, 85m, 80m, 4);
        result.AdaptationKcal.Should().Be(0m);
    }

    [Fact]
    public void EstimateAdaptation_5kgLossOver8Weeks()
    {
        // weight-based: 5 * 15 = 75; time-based: 8/4 * 50 = 100; total = 175
        var result = MetabolicCalculatorService.EstimateAdaptation(2500m, 75m, 80m, 8);
        result.AdaptationKcal.Should().Be(175.0m);
        result.AdaptedTdeeKcal.Should().Be(2325.0m);
        result.AdaptationPercent.Should().Be(7.0m);
    }

    [Fact]
    public void EstimateAdaptation_ClampsAt20Percent()
    {
        // huge loss: 30kg * 15 = 450; time: 20/4*50=250; total=700
        // 20% of 2500 = 500 → clamped
        var result = MetabolicCalculatorService.EstimateAdaptation(2500m, 50m, 80m, 20);
        result.AdaptationKcal.Should().Be(500.0m);
        result.AdaptationPercent.Should().Be(20.0m);
        result.AdaptedTdeeKcal.Should().Be(2000.0m);
    }

    // --- EstimateNeatCompensation ---

    [Fact]
    public void EstimateNeatCompensation_AtBaseline_ZeroCompensation()
    {
        var result = MetabolicCalculatorService.EstimateNeatCompensation(8000);
        result.CompensationKcal.Should().Be(0m);
        result.CompensationPercent.Should().Be(0m);
    }

    [Fact]
    public void EstimateNeatCompensation_AboveBaseline_ZeroCompensation()
    {
        var result = MetabolicCalculatorService.EstimateNeatCompensation(12000);
        result.CompensationKcal.Should().Be(0m);
        result.CurrentNeatKcal.Should().Be(480.0m);
    }

    [Fact]
    public void EstimateNeatCompensation_BelowBaseline_PositiveCompensation()
    {
        // baseline 8000 * 0.04 = 320; current 5000 * 0.04 = 200; comp = 3000 * 0.04 = 120
        var result = MetabolicCalculatorService.EstimateNeatCompensation(5000);
        result.CompensationKcal.Should().Be(120.0m);
        result.BaselineNeatKcal.Should().Be(320.0m);
        result.CurrentNeatKcal.Should().Be(200.0m);
        result.CompensationPercent.Should().Be(37.5m);
    }

    // --- CalculateIntakeTarget ---

    [Fact]
    public void CalculateIntakeTarget_NormalDeficit()
    {
        // 0.5 kg/wk * 1100 = 550 daily deficit; 2500 - 550 = 1950
        var result = MetabolicCalculatorService.CalculateIntakeTarget(2500m, 0.5m, 1500m);
        result.TargetKcal.Should().Be(1950.0m);
        result.DeficitKcal.Should().Be(550.0m);
    }

    [Fact]
    public void CalculateIntakeTarget_ClampsAtBmrFloor()
    {
        // 2.0 kg/wk * 1100 = 2200 deficit; 2500 - 2200 = 300 → clamp to 1500 BMR
        var result = MetabolicCalculatorService.CalculateIntakeTarget(2500m, 2.0m, 1500m);
        result.TargetKcal.Should().Be(1500.0m);
        result.DeficitKcal.Should().Be(1000.0m); // actual = 2500-1500
    }

    // --- CalculateWeeklyRate ---

    [Fact]
    public void CalculateWeeklyRate_Losing_ReturnsNegative()
    {
        MetabolicCalculatorService.CalculateWeeklyRate(80.5m, 80.0m).Should().Be(-0.50m);
    }

    [Fact]
    public void CalculateWeeklyRate_Gaining_ReturnsPositive()
    {
        MetabolicCalculatorService.CalculateWeeklyRate(80.0m, 80.7m).Should().Be(0.70m);
    }

    [Fact]
    public void CalculateWeeklyRate_Stable_ReturnsZero()
    {
        MetabolicCalculatorService.CalculateWeeklyRate(80.0m, 80.0m).Should().Be(0.0m);
    }

    // --- ProjectWeight ---

    [Fact]
    public void ProjectWeight_4Weeks_Returns4Projections()
    {
        var result = MetabolicCalculatorService.ProjectWeight(80m, -0.5m, 4);
        result.Should().HaveCount(4);
        result[0].ProjectedWeightKg.Should().Be(79.5m);
        result[1].ProjectedWeightKg.Should().Be(79.0m);
        result[2].ProjectedWeightKg.Should().Be(78.5m);
        result[3].ProjectedWeightKg.Should().Be(78.0m);
    }

    [Fact]
    public void ProjectWeight_ZeroWeeks_ReturnsEmpty()
    {
        var result = MetabolicCalculatorService.ProjectWeight(80m, -0.5m, 0);
        result.Should().BeEmpty();
    }

    // --- CalculateEma ---

    [Fact]
    public void CalculateEma_7DayPeriod_CorrectSmoothing()
    {
        // alpha = 2/(7+1) = 0.25; EMA = 0.25*81 + 0.75*80 = 20.25 + 60 = 80.25
        var result = MetabolicCalculatorService.CalculateEma(80m, 81m, 7);
        result.Should().Be(80.25m);
    }

    [Fact]
    public void CalculateEma_30DayPeriod_LessResponsive()
    {
        // alpha = 2/31 ≈ 0.0645; EMA ≈ 0.0645*81 + 0.9355*80 ≈ 5.2245 + 74.84 ≈ 80.06
        var result = MetabolicCalculatorService.CalculateEma(80m, 81m, 30);
        result.Should().BeGreaterThanOrEqualTo(80.06m);
        result.Should().BeLessThanOrEqualTo(80.07m);
    }
}
