using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/partners")
            .WithTags("Partners")
            .RequireAuthorization()
            .RequireTier(SubscriptionTier.Competitor);

        group.MapPost("/invite", CreateInvite)
            .WithName("CreateInvite")
            .WithSummary("Create a partner invite code");

        group.MapPost("/accept", AcceptInvite)
            .WithName("AcceptInvite")
            .WithSummary("Accept a partner invite")
            .WithValidation<AcceptInviteRequest>();

        group.MapPost("/decline", DeclineInvite)
            .WithName("DeclineInvite")
            .WithSummary("Decline a partner invite")
            .WithValidation<DeclineInviteRequest>();

        group.MapGet("/", ListPartnerships)
            .WithName("ListPartnerships")
            .WithSummary("List all partnerships");

        group.MapGet("/active", GetActivePartnership)
            .WithName("GetActivePartnership")
            .WithSummary("Get current active partnership");

        group.MapGet("/{id:guid}", GetPartnership)
            .WithName("GetPartnership")
            .WithSummary("Get partnership by ID");

        group.MapPost("/{id:guid}/end", EndPartnership)
            .WithName("EndPartnership")
            .WithSummary("End an active partnership");
    }

    private static async Task<IResult> CreateInvite(
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnership = await partnerService.CreateInviteAsync(currentUser.UserId, ct);
        return Results.Ok(new CreateInviteResponse(
            partnership.Id,
            partnership.InviteCode,
            partnership.ExpiresAt!.Value.ToString("o")));
    }

    private static async Task<IResult> AcceptInvite(
        AcceptInviteRequest req,
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnership = await partnerService.AcceptInviteAsync(currentUser.UserId, req.InviteCode, ct);
        return Results.Ok(ToResponse(partnership));
    }

    private static async Task<IResult> DeclineInvite(
        DeclineInviteRequest req,
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        await partnerService.DeclineInviteAsync(currentUser.UserId, req.InviteCode, ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListPartnerships(
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnerships = await partnerService.GetPartnershipsAsync(currentUser.UserId, ct);
        var items = partnerships.Select(ToResponse).ToList();
        return Results.Ok(new PartnershipListResponse(items, items.Count));
    }

    private static async Task<IResult> GetActivePartnership(
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnership = await partnerService.GetActivePartnershipAsync(currentUser.UserId, ct);
        if (partnership is null)
            return Results.NotFound();
        return Results.Ok(ToResponse(partnership));
    }

    private static async Task<IResult> GetPartnership(
        Guid id,
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnership = await partnerService.GetPartnershipAsync(currentUser.UserId, id, ct);
        if (partnership is null)
            return Results.NotFound();
        return Results.Ok(ToResponse(partnership));
    }

    private static async Task<IResult> EndPartnership(
        Guid id,
        ICurrentUserService currentUser,
        IPartnershipService partnerService,
        CancellationToken ct)
    {
        var partnership = await partnerService.EndPartnershipAsync(currentUser.UserId, id, ct);
        return Results.Ok(ToResponse(partnership));
    }

    private static PartnershipResponse ToResponse(Partnership p)
    {
        return new PartnershipResponse(
            p.Id,
            p.RequesterId,
            p.Requester?.DisplayName ?? "Unknown",
            p.ResponderId,
            p.Responder?.DisplayName,
            p.Status.ToString(),
            p.Status == PartnershipStatus.Pending ? p.InviteCode : null,
            p.ExpiresAt?.ToString("o"),
            p.CreatedAt);
    }
}
