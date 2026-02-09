using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class TrainingDayEndpoints
{
    public static void MapTrainingDayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/training-day")
            .WithTags("Training Day")
            .RequireAuthorization();

        group.MapGet("/", GetTrainingDaySummary)
            .WithName("GetTrainingDaySummary")
            .WithSummary("Get training day summary with interference analysis");
    }

    private static async Task<IResult> GetTrainingDaySummary(
        string date,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var targetDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var userId = currentUser.UserId;

        // Get completed workouts for the date (with sets + exercises for muscle groups)
        var workouts = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
                .ThenInclude(s => s.Exercise)
            .Where(w => w.UserId == userId
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) == targetDate
                && w.Status == WorkoutStatus.Completed)
            .ToListAsync(ct);

        // Get cardio sessions for the date
        var cardioSessions = await cardioService.GetCardioForDateAsync(userId, targetDate, ct);

        // Calculate strength TRIMP
        var strengthTrimp = 0m;
        var muscleGroups = new List<string>();
        foreach (var workout in workouts)
        {
            var sets = workout.Sets.Select(s => (s.ActualWeight, s.ActualReps, s.Rpe, s.Rir, s.IsWarmup));
            var stress = TrainingStressService.CalculateSessionStress(sets);
            strengthTrimp += stress.Trimp;

            foreach (var set in workout.Sets)
            {
                if (set.Exercise?.PrimaryMuscles is not null)
                    muscleGroups.AddRange(set.Exercise.PrimaryMuscles);
            }
        }
        muscleGroups = muscleGroups.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Calculate cardio TRIMP (already stored on sessions)
        var cardioTrimp = cardioSessions.Sum(c => c.CardioTrimp ?? 0m);

        // Calculate interference score (use highest from all cardio sessions)
        var maxInterference = 0m;
        var maxInterferenceLevel = "Low";
        CardioModality? primaryModality = null;
        CardioIntensityZone? primaryZone = null;

        foreach (var cardio in cardioSessions)
        {
            var interference = InterferenceCalculatorService.CalculateInterferenceScore(
                cardio.Modality, cardio.Zone, cardio.DurationMinutes, muscleGroups);

            if (interference.Score > maxInterference)
            {
                maxInterference = interference.Score;
                maxInterferenceLevel = interference.Level;
                primaryModality = cardio.Modality;
                primaryZone = cardio.Zone;
            }
        }

        // Sequencing recommendation
        var sequencing = InterferenceCalculatorService.RecommendSequencing(
            workouts.Count > 0, cardioSessions.Count > 0, primaryModality, primaryZone);

        // Weekly data for rule engine
        var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek + 1); // Monday
        if (targetDate.DayOfWeek == DayOfWeek.Sunday)
            weekStart = targetDate.AddDays(-6);
        var weekEnd = weekStart.AddDays(6);

        var weeklyCardio = await db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= weekStart && c.Date <= weekEnd)
            .ToListAsync(ct);

        var weeklyCardioTrimp = weeklyCardio.Sum(c => c.CardioTrimp ?? 0m);

        var weeklyWorkouts = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .Where(w => w.UserId == userId
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) >= weekStart
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) <= weekEnd
                && w.Status == WorkoutStatus.Completed)
            .ToListAsync(ct);

        var weeklyStrengthTrimp = 0m;
        foreach (var w in weeklyWorkouts)
        {
            var sets = w.Sets.Select(s => (s.ActualWeight, s.ActualReps, s.Rpe, s.Rir, s.IsWarmup));
            weeklyStrengthTrimp += TrainingStressService.CalculateSessionStress(sets).Trimp;
        }

        // Zone 3+ percentage
        var totalCardioMinutes = weeklyCardio.Sum(c => c.DurationMinutes);
        var zone3PlusMinutes = weeklyCardio.Where(c => c.Zone >= CardioIntensityZone.Zone3).Sum(c => c.DurationMinutes);
        var weeklyZone3PlusPct = totalCardioMinutes > 0 ? zone3PlusMinutes * 100 / totalCardioMinutes : 0;

        // Calculate separation hours (approximate from StartedAt)
        decimal? separationHours = null;
        if (workouts.Count > 0 && cardioSessions.Any(c => c.StartedAt.HasValue))
        {
            var lastWorkoutEnd = workouts.Max(w => w.CompletedAt ?? w.ScheduledAt);
            var firstCardioStart = cardioSessions.Where(c => c.StartedAt.HasValue).Min(c => c.StartedAt!.Value);
            var gap = Math.Abs((firstCardioStart - lastWorkoutEnd).TotalHours);
            separationHours = (decimal)gap;
        }

        // Run rule engine
        var ruleAlerts = ConcurrentTrainingRuleEngine.EvaluateRules(
            maxInterference, workouts.Count > 0, cardioSessions.Count > 0,
            separationHours, primaryZone, primaryModality, muscleGroups,
            weeklyCardioTrimp, weeklyStrengthTrimp, weeklyZone3PlusPct);

        var alerts = ruleAlerts.Select(a => new CardioAlertResponse(a.RuleName, a.Severity.ToString(), a.Message)).ToList();

        return Results.Ok(new TrainingDaySummaryResponse(
            targetDate.ToString("yyyy-MM-dd"),
            workouts.Count, cardioSessions.Count,
            strengthTrimp, cardioTrimp, strengthTrimp + cardioTrimp,
            maxInterference, maxInterferenceLevel,
            new SequencingResponse(sequencing.RecommendedOrder, sequencing.SeparationHours, sequencing.Rationale),
            alerts));
    }
}
