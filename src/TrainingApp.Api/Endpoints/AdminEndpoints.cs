using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization()
            .RequireAdmin();

        // Dashboard
        group.MapGet("/stats", GetSystemStats)
            .WithName("GetSystemStats")
            .WithSummary("System KPIs dashboard");

        // Users
        group.MapGet("/users", GetUsers)
            .WithName("AdminGetUsers")
            .WithSummary("Paginated user list with filters");

        group.MapGet("/users/{id:guid}", GetUserDetail)
            .WithName("AdminGetUserDetail")
            .WithSummary("User detail with profile, subscription, activity");

        group.MapPost("/users/{id:guid}/role", ChangeUserRole)
            .WithName("AdminChangeUserRole")
            .WithSummary("Change user role")
            .WithValidation<ChangeRoleRequest>();

        // Subscriptions
        group.MapGet("/subscriptions", GetSubscriptions)
            .WithName("AdminGetSubscriptions")
            .WithSummary("Paginated subscription list with filters");

        group.MapPost("/subscriptions/{id:guid}/override", OverrideTier)
            .WithName("AdminOverrideTier")
            .WithSummary("Override subscription tier")
            .WithValidation<OverrideTierRequest>();

        group.MapPost("/subscriptions/{id:guid}/extend", ExtendTrial)
            .WithName("AdminExtendTrial")
            .WithSummary("Extend trial end date")
            .WithValidation<ExtendTrialRequest>();

        group.MapPost("/subscriptions/{id:guid}/cancel", AdminCancelSubscription)
            .WithName("AdminCancelSubscription")
            .WithSummary("Admin cancel subscription")
            .WithValidation<AdminCancelRequest>();

        // Payments
        group.MapGet("/payments/failures", GetPaymentFailures)
            .WithName("AdminGetPaymentFailures")
            .WithSummary("Users with failed payments");

        // Activity
        group.MapGet("/activity", GetRecentActivity)
            .WithName("AdminGetRecentActivity")
            .WithSummary("Recent platform activity feed");

        // Coach Applications (existing)
        group.MapGet("/coach-applications", GetPendingApplications)
            .WithName("GetPendingCoachApplications")
            .WithSummary("List pending coach applications");

        group.MapPost("/coach-applications/{id:guid}/review", ReviewApplication)
            .WithName("ReviewCoachApplication")
            .WithSummary("Approve or reject a coach application");
    }

    private static async Task<IResult> GetSystemStats(
        IAdminService adminService, CancellationToken ct)
    {
        var stats = await adminService.GetSystemStatsAsync(ct);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetUsers(
        IAdminService adminService,
        string? q, string? role, string? tier, string? status,
        int page = 1, int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await adminService.GetUsersAsync(q, role, tier, status, page, pageSize, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserDetail(
        Guid id, IAdminService adminService, CancellationToken ct)
    {
        var detail = await adminService.GetUserDetailAsync(id, ct);
        return Results.Ok(detail);
    }

    private static async Task<IResult> ChangeUserRole(
        Guid id, ChangeRoleRequest request,
        ICurrentUserService currentUser,
        IAdminService adminService, CancellationToken ct)
    {
        await adminService.ChangeUserRoleAsync(currentUser.UserId, id, request.Role, ct);
        return Results.Ok(new { message = $"Role changed to {request.Role}" });
    }

    private static async Task<IResult> GetSubscriptions(
        IAdminService adminService,
        string? status, string? tier,
        int page = 1, int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await adminService.GetSubscriptionsAsync(status, tier, page, pageSize, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> OverrideTier(
        Guid id, OverrideTierRequest request,
        ICurrentUserService currentUser,
        IAdminService adminService, CancellationToken ct)
    {
        await adminService.OverrideTierAsync(currentUser.UserId, id, request.Tier, request.Reason, ct);
        return Results.Ok(new { message = $"Tier overridden to {request.Tier}" });
    }

    private static async Task<IResult> ExtendTrial(
        Guid id, ExtendTrialRequest request,
        ICurrentUserService currentUser,
        IAdminService adminService, CancellationToken ct)
    {
        await adminService.ExtendTrialAsync(currentUser.UserId, id, request.Days, ct);
        return Results.Ok(new { message = $"Trial extended by {request.Days} days" });
    }

    private static async Task<IResult> AdminCancelSubscription(
        Guid id, AdminCancelRequest request,
        ICurrentUserService currentUser,
        IAdminService adminService, CancellationToken ct)
    {
        await adminService.AdminCancelSubscriptionAsync(currentUser.UserId, id, request.Reason, ct);
        return Results.Ok(new { message = "Subscription cancelled" });
    }

    private static async Task<IResult> GetPaymentFailures(
        IAdminService adminService,
        int page = 1, int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await adminService.GetPaymentFailuresAsync(page, pageSize, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecentActivity(
        IAdminService adminService,
        int count = 50,
        CancellationToken ct = default)
    {
        var activity = await adminService.GetRecentActivityAsync(count, ct);
        return Results.Ok(activity);
    }

    private static async Task<IResult> GetPendingApplications(
        ICoachApplicationService applicationService,
        CancellationToken ct)
    {
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
