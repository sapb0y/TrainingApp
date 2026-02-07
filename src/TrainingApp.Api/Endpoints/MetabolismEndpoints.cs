using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class MetabolismEndpoints
{
    public static void MapMetabolismEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/metabolism")
            .WithTags("Metabolism");

        group.MapGet("/summary", GetMetabolismSummary)
            .WithName("GetMetabolismSummary")
            .WithSummary("Get comprehensive metabolism summary with BMR, TDEE, adaptation, and alerts");
    }

    private static async Task<IResult> GetMetabolismSummary(
        ICurrentUserService currentUser,
        IWeightTrackingService weightService,
        IDeficitPhaseService deficitService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Results.NotFound();

        var settings = user.Settings;
        var latestWeight = await weightService.GetLatestWeightAsync(userId, ct);

        // Calculate BMR/TDEE if we have enough profile data
        decimal? bmrKcal = null;
        decimal? tdeeKcal = null;
        decimal? adjustedTdee = null;
        decimal? adaptationKcal = null;
        decimal? adaptationPercent = null;
        decimal? neatCompensationKcal = null;
        decimal? intakeTarget = null;
        decimal? deficitKcal = null;

        if (settings.Sex.HasValue && settings.HeightCm.HasValue && settings.DateOfBirth.HasValue && latestWeight is not null)
        {
            var age = CalculateAge(settings.DateOfBirth.Value);
            var bmr = MetabolicCalculatorService.CalculateBmr(settings.Sex.Value, latestWeight.WeightKg, settings.HeightCm.Value, age);
            bmrKcal = bmr.BmrKcal;

            var tdee = MetabolicCalculatorService.CalculateTdee(bmr.BmrKcal, settings.ActivityLevel);
            tdeeKcal = tdee.TdeeKcal;
            adjustedTdee = tdee.TdeeKcal;

            // Check for active deficit
            var activeDeficit = await deficitService.GetActiveDeficitAsync(userId, ct);
            if (activeDeficit is not null)
            {
                var weeksInDeficit = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - activeDeficit.StartDate.DayNumber) / 7;

                // Metabolic adaptation
                var adaptation = MetabolicCalculatorService.EstimateAdaptation(
                    tdee.TdeeKcal, latestWeight.WeightKg, activeDeficit.StartWeightKg, weeksInDeficit);
                adaptationKcal = adaptation.AdaptationKcal;
                adaptationPercent = adaptation.AdaptationPercent;
                adjustedTdee = adaptation.AdaptedTdeeKcal;

                // NEAT compensation from recent logs
                var recentNeat = await db.NeatLogs
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.Date)
                    .Take(7)
                    .ToListAsync(ct);

                if (recentNeat.Count > 0)
                {
                    var avgSteps = (int)recentNeat.Average(n => n.StepCount);
                    var neatComp = MetabolicCalculatorService.EstimateNeatCompensation(avgSteps);
                    neatCompensationKcal = neatComp.CompensationKcal;
                    adjustedTdee -= neatComp.CompensationKcal;
                }

                // Intake target
                var intake = MetabolicCalculatorService.CalculateIntakeTarget(adjustedTdee.Value, activeDeficit.WeeklyRateKg, bmr.BmrKcal);
                intakeTarget = intake.TargetKcal;
                deficitKcal = intake.DeficitKcal;
            }
        }

        // Projections
        var projections = new List<WeightProjectionResponse>();
        if (latestWeight is not null)
        {
            var rate = latestWeight.WeeklyRateKg ?? 0m;
            var projected = MetabolicCalculatorService.ProjectWeight(latestWeight.WeightKg, rate, 12);
            projections = projected.Select(p => new WeightProjectionResponse(p.Date.ToString("yyyy-MM-dd"), p.ProjectedWeightKg)).ToList();
        }

        // Fat loss rule engine alerts
        var alerts = new List<FatLossAlertResponse>();
        var deficit = await deficitService.GetActiveDeficitAsync(userId, ct);
        if (deficit is not null || latestWeight is not null)
        {
            var isActive = deficit is not null;
            var weeksIn = isActive ? (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - deficit!.StartDate.DayNumber) / 7 : 0;

            int? daysSinceBreak = null;
            int? breakIntervalDays = null;
            if (deficit is not null)
            {
                var breakRef = deficit.LastDietBreakDate ?? deficit.StartDate;
                daysSinceBreak = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - breakRef.DayNumber;
                breakIntervalDays = deficit.DietBreakIntervalWeeks.HasValue ? deficit.DietBreakIntervalWeeks.Value * 7 : null;
            }

            var ruleAlerts = FatLossRuleEngine.EvaluateRules(
                latestWeight?.WeeklyRateKg,
                weeksIn,
                isActive,
                adaptationPercent ?? 0m,
                neatCompensationKcal.HasValue && tdeeKcal.HasValue && tdeeKcal > 0
                    ? neatCompensationKcal.Value / tdeeKcal.Value * 100m
                    : 0m,
                daysSinceBreak,
                breakIntervalDays);

            alerts = ruleAlerts.Select(a => new FatLossAlertResponse(a.RuleName, a.Severity.ToString(), a.Message)).ToList();
        }

        return Results.Ok(new MetabolismSummaryResponse(
            bmrKcal, tdeeKcal, adjustedTdee,
            adaptationKcal, adaptationPercent,
            neatCompensationKcal, intakeTarget, deficitKcal,
            latestWeight?.WeightKg, latestWeight?.WeeklyRateKg,
            projections, alerts));
    }

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
            age--;
        return age;
    }
}
