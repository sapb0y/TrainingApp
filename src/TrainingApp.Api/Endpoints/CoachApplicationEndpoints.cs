using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class CoachApplicationEndpoints
{
    public static void MapCoachApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/coach-applications")
            .WithTags("Coach Applications")
            .RequireAuthorization();

        group.MapPost("/", SubmitApplication)
            .WithName("SubmitCoachApplication")
            .WithSummary("Submit a coach application")
            .WithValidation<SubmitCoachApplicationRequest>();

        group.MapGet("/mine", GetMyApplication)
            .WithName("GetMyCoachApplication")
            .WithSummary("Check own application status");
    }

    private static async Task<IResult> SubmitApplication(
        SubmitCoachApplicationRequest request,
        ICurrentUserService currentUser,
        ICoachApplicationService applicationService,
        CancellationToken ct)
    {
        var application = await applicationService.SubmitApplicationAsync(
            currentUser.UserId, request.Credentials, request.CurrentClientCount,
            request.BusinessGoal, request.AdditionalInfo, ct);

        return Results.Created($"/api/v1/coach-applications/mine", ToResponse(application));
    }

    private static async Task<IResult> GetMyApplication(
        ICurrentUserService currentUser,
        ICoachApplicationService applicationService,
        CancellationToken ct)
    {
        var application = await applicationService.GetApplicationAsync(currentUser.UserId, ct);

        if (application is null)
            return Results.NotFound();

        return Results.Ok(ToResponse(application));
    }

    private static CoachApplicationResponse ToResponse(Core.Entities.CoachApplication a) => new(
        a.Id, a.UserId, a.User?.DisplayName, a.Credentials, a.CurrentClientCount,
        a.BusinessGoal, a.AdditionalInfo, a.Status.ToString(),
        a.ReviewNotes, a.ReviewedAt, a.CreatedAt);
}
