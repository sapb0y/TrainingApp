using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class CoachEndpoints
{
    public static void MapCoachEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/coach")
            .WithTags("Coach");

        group.MapPost("/invite", CreateInvite)
            .WithName("CreateCoachInvite")
            .WithSummary("Create a coach invite code for an athlete");

        group.MapPost("/accept", AcceptInvite)
            .WithName("AcceptCoachInvite")
            .WithSummary("Accept a coach invite")
            .WithValidation<AcceptCoachInviteRequest>();

        group.MapGet("/athletes", ListAthletes)
            .WithName("ListCoachAthletes")
            .WithSummary("List all coached athletes");

        group.MapGet("/athletes/{id:guid}", GetAthlete)
            .WithName("GetCoachAthlete")
            .WithSummary("Get coached athlete details");

        group.MapPost("/athletes/{id:guid}/end", EndRelationship)
            .WithName("EndCoachRelationship")
            .WithSummary("End coaching relationship with athlete");

        group.MapPut("/athletes/{id:guid}/permissions", UpdatePermissions)
            .WithName("UpdateCoachPermissions")
            .WithSummary("Update permissions for a coached athlete")
            .WithValidation<UpdatePermissionsRequest>();
    }

    private static async Task<IResult> CreateInvite(
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can create invites.");

        var relationship = await coachService.CreateInviteAsync(currentUser.UserId, ct);
        return Results.Ok(new CoachInviteResponse(
            relationship.Id,
            relationship.InviteCode,
            relationship.ExpiresAt!.Value.ToString("o")));
    }

    private static async Task<IResult> AcceptInvite(
        AcceptCoachInviteRequest req,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        var relationship = await coachService.AcceptInviteAsync(currentUser.UserId, req.InviteCode, ct);
        return Results.Ok(ToResponse(relationship));
    }

    private static async Task<IResult> ListAthletes(
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athletes.");

        var athletes = await coachService.GetAthletesAsync(currentUser.UserId, ct);
        var items = athletes.Select(ToResponse).ToList();
        return Results.Ok(new CoachAthleteListResponse(items, items.Count));
    }

    private static async Task<IResult> GetAthlete(
        Guid id,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athletes.");

        var relationship = await coachService.GetRelationshipAsync(currentUser.UserId, id, ct);
        if (relationship is null)
            return Results.NotFound();
        return Results.Ok(ToResponse(relationship));
    }

    private static async Task<IResult> EndRelationship(
        Guid id,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can end relationships.");

        var relationship = await coachService.EndRelationshipAsync(currentUser.UserId, id, ct);
        return Results.Ok(ToResponse(relationship));
    }

    private static async Task<IResult> UpdatePermissions(
        Guid id,
        UpdatePermissionsRequest req,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can update permissions.");

        // Get current relationship to merge permissions
        var existing = await coachService.GetRelationshipAsync(currentUser.UserId, id, ct)
            ?? throw new Core.Exceptions.NotFoundException("CoachAthlete");

        var perms = new CoachPermissions
        {
            CanModifyProgram = req.CanModifyProgram ?? existing.Permissions.CanModifyProgram,
            CanViewMetrics = req.CanViewMetrics ?? existing.Permissions.CanViewMetrics,
            CanAddNotes = req.CanAddNotes ?? existing.Permissions.CanAddNotes
        };

        var relationship = await coachService.UpdatePermissionsAsync(currentUser.UserId, id, perms, ct);
        return Results.Ok(ToResponse(relationship));
    }

    private static CoachAthleteResponse ToResponse(CoachAthlete c)
    {
        return new CoachAthleteResponse(
            c.Id,
            c.CoachId,
            c.Coach?.DisplayName ?? "Unknown",
            c.AthleteId,
            c.Athlete?.DisplayName,
            c.Status.ToString(),
            new CoachPermissionsResponse(
                c.Permissions.CanModifyProgram,
                c.Permissions.CanViewMetrics,
                c.Permissions.CanAddNotes),
            c.Status == CoachAthleteStatus.Pending ? c.InviteCode : null,
            c.ExpiresAt?.ToString("o"),
            c.CreatedAt);
    }
}
