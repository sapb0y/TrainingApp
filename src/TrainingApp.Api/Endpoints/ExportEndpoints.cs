using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/export")
            .WithTags("Export")
            .RequireAuthorization();

        group.MapGet("/workouts", ExportWorkouts)
            .WithName("ExportWorkouts")
            .WithSummary("Export workouts as CSV");

        group.MapGet("/weight", ExportWeightLogs)
            .WithName("ExportWeightLogs")
            .WithSummary("Export weight logs as CSV");

        group.MapGet("/cardio", ExportCardioSessions)
            .WithName("ExportCardioSessions")
            .WithSummary("Export cardio sessions as CSV");
    }

    private static async Task<IResult> ExportWorkouts(
        string from, string to,
        ICurrentUserService currentUser,
        IExportService exportService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var bytes = await exportService.ExportWorkoutsCsvAsync(currentUser.UserId, fromDate, toDate, ct);
        return Results.File(bytes, "text/csv", "workouts.csv");
    }

    private static async Task<IResult> ExportWeightLogs(
        string from, string to,
        ICurrentUserService currentUser,
        IExportService exportService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var bytes = await exportService.ExportWeightLogsCsvAsync(currentUser.UserId, fromDate, toDate, ct);
        return Results.File(bytes, "text/csv", "weight-logs.csv");
    }

    private static async Task<IResult> ExportCardioSessions(
        string from, string to,
        ICurrentUserService currentUser,
        IExportService exportService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var bytes = await exportService.ExportCardioSessionsCsvAsync(currentUser.UserId, fromDate, toDate, ct);
        return Results.File(bytes, "text/csv", "cardio-sessions.csv");
    }
}
