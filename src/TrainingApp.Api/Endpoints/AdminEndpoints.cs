using TrainingApp.Api.Contracts;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        group.MapGet("/coach-applications", GetPendingApplications)
            .WithName("GetPendingCoachApplications")
            .WithSummary("List pending coach applications (admin only)");

        group.MapPost("/coach-applications/{id:guid}/review", ReviewApplication)
            .WithName("ReviewCoachApplication")
            .WithSummary("Approve or reject a coach application (admin only)");
    }

    private static async Task<IResult> GetPendingApplications(
        ICurrentUserService currentUser,
        ICoachApplicationService applicationService,
        CancellationToken ct)
    {
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("Admin access required");

        var applications = await applicationService.GetPendingApplicationsAsync(ct);

        return Results.Ok(applications.Select(a => new CoachApplicationResponse(
            a.Id, a.UserId, a.User?.DisplayName, a.Credentials, a.CurrentClientCount,
            a.BusinessGoal, a.AdditionalInfo, a.Status.ToString(),
            a.ReviewNotes, a.ReviewedAt, a.CreatedAt)));
    }

    private static async Task<IResult> ReviewApplication(
        Guid id,
        ReviewCoachApplicationRequest request,
        ICurrentUserService currentUser,
        ICoachApplicationService applicationService,
        CancellationToken ct)
    {
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("Admin access required");

        var application = await applicationService.ReviewApplicationAsync(
            id, currentUser.UserId, request.Approve, request.Notes, ct);

        return Results.Ok(new CoachApplicationResponse(
            application.Id, application.UserId, application.User?.DisplayName,
            application.Credentials, application.CurrentClientCount,
            application.BusinessGoal, application.AdditionalInfo,
            application.Status.ToString(), application.ReviewNotes,
            application.ReviewedAt, application.CreatedAt));
    }
}
