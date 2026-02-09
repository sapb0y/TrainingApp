using TrainingApp.Api.Contracts;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class InsightEndpoints
{
    public static void MapInsightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/insights")
            .WithTags("Insights")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateInsights)
            .WithName("GenerateInsights")
            .WithSummary("Generate fresh insights by running the rule engine");

        group.MapGet("/", GetInsightHistory)
            .WithName("GetInsightHistory")
            .WithSummary("Get insight history with optional date range and category filter");
    }

    private static async Task<IResult> GenerateInsights(
        ICurrentUserService currentUser,
        IInsightGeneratorService insightService,
        CancellationToken ct)
    {
        var insights = await insightService.GenerateInsightsAsync(currentUser.UserId, ct);
        var items = insights.Select(ToResponse).ToList();
        return Results.Ok(new InsightListResponse(items, items.Count));
    }

    private static async Task<IResult> GetInsightHistory(
        string? from, string? to, string? category,
        ICurrentUserService currentUser,
        IInsightGeneratorService insightService,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from is not null && DateOnly.TryParseExact(from, "yyyy-MM-dd", out var f)
            ? f : today.AddDays(-30);
        var toDate = to is not null && DateOnly.TryParseExact(to, "yyyy-MM-dd", out var t)
            ? t : today;

        var insights = await insightService.GetInsightHistoryAsync(currentUser.UserId, fromDate, toDate, category, ct);
        var items = insights.Select(ToResponse).ToList();
        return Results.Ok(new InsightListResponse(items, items.Count));
    }

    private static InsightResponse ToResponse(Core.Entities.Insight i) => new(
        i.Id, i.RuleName, i.Category, i.Severity, i.Title, i.Message,
        i.GeneratedDate.ToString("yyyy-MM-dd"), i.RelatedEntityType, i.RelatedEntityId);
}
