using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class NeatEndpoints
{
    public static void MapNeatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/neat")
            .WithTags("NEAT")
            .RequireAuthorization();

        group.MapPost("/", LogNeat)
            .WithName("LogNeat")
            .WithSummary("Log daily step count")
            .WithValidation<LogNeatRequest>();

        group.MapGet("/", GetNeatHistory)
            .WithName("GetNeatHistory")
            .WithSummary("Get NEAT logs for a date range");
    }

    private static async Task<IResult> LogNeat(
        LogNeatRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");

        var existing = await db.NeatLogs
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Date == date, ct);

        if (existing is not null)
        {
            existing.StepCount = req.StepCount;
            existing.Notes = req.Notes;
        }
        else
        {
            existing = new NeatLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                StepCount = req.StepCount,
                Notes = req.Notes
            };
            db.NeatLogs.Add(existing);
        }

        // Calculate NEAT compensation
        var comp = MetabolicCalculatorService.EstimateNeatCompensation(req.StepCount);
        existing.EstimatedNeatKcal = comp.CurrentNeatKcal;
        existing.CompensationPercent = comp.CompensationPercent;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(existing));
    }

    private static async Task<IResult> GetNeatHistory(
        string from,
        string to,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var logs = await db.NeatLogs
            .AsNoTracking()
            .Where(n => n.UserId == currentUser.UserId && n.Date >= fromDate && n.Date <= toDate)
            .OrderBy(n => n.Date)
            .ToListAsync(ct);

        var items = logs.Select(ToResponse).ToList();
        return Results.Ok(new NeatLogListResponse(items, items.Count));
    }

    private static NeatLogResponse ToResponse(NeatLog n) => new(
        n.Id, n.Date.ToString("yyyy-MM-dd"), n.StepCount,
        n.EstimatedNeatKcal, n.CompensationPercent, n.Notes, n.CreatedAt);
}
