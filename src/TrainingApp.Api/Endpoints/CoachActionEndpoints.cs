using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;
using ProgramEntity = TrainingApp.Core.Entities.Program;

namespace TrainingApp.Api.Endpoints;

public static class CoachActionEndpoints
{
    public static void MapCoachActionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/coach")
            .WithTags("Coach Actions");

        group.MapPost("/athletes/{id:guid}/programs/generate", GenerateAthleteProgram)
            .WithName("GenerateAthleteProgram")
            .WithSummary("Generate a program for an athlete")
            .WithValidation<GenerateProgramRequest>();

        group.MapGet("/athletes/{id:guid}/programs", ListAthletePrograms)
            .WithName("ListAthletePrograms")
            .WithSummary("List athlete programs");

        group.MapPut("/athletes/{id:guid}/workouts/{wid:guid}/sets/{sid:guid}", ModifyAthleteSet)
            .WithName("ModifyAthleteSet")
            .WithSummary("Modify a planned set for an athlete")
            .WithValidation<CoachModifySetRequest>();

        group.MapPost("/athletes/{id:guid}/notes", AddNote)
            .WithName("AddCoachNote")
            .WithSummary("Add a contextual note for an athlete")
            .WithValidation<CoachNoteRequest>();

        group.MapGet("/athletes/{id:guid}/notes", ListNotes)
            .WithName("ListCoachNotes")
            .WithSummary("List coach notes for an athlete");

        group.MapDelete("/athletes/{id:guid}/notes/{nid:guid}", DeleteNote)
            .WithName("DeleteCoachNote")
            .WithSummary("Delete a coach note");
    }

    private static async Task<IResult> GenerateAthleteProgram(
        Guid id,
        GenerateProgramRequest req,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        IProgramGeneratorService generator,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can generate programs for athletes.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var rel = await coachService.GetRelationshipAsync(currentUser.UserId, id, ct)
            ?? throw new NotFoundException("CoachAthlete");
        if (!rel.Permissions.CanModifyProgram)
            throw new ForbiddenException("You don't have permission to modify this athlete's program.");

        var goal = Enum.Parse<ProgramGoal>(req.Goal, true);
        var template = Enum.Parse<ProgramTemplate>(req.Template, true);
        var startDate = DateOnly.Parse(req.StartDate);

        var program = await generator.GenerateProgramAsync(
            id, req.Name, goal, template, req.DurationWeeks, startDate, ct);

        return Results.Created($"/api/v1/programs/{program.Id}", new ProgramSummaryResponse(
            program.Id, program.Name, program.Goal.ToString(), program.Template.ToString(),
            program.Status.ToString(), program.DurationWeeks, program.CreatedAt));
    }

    private static async Task<IResult> ListAthletePrograms(
        Guid id,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athlete programs.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var programs = await db.Programs
            .AsNoTracking()
            .Where(p => p.UserId == id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProgramSummaryResponse(
                p.Id, p.Name, p.Goal.ToString(), p.Template.ToString(),
                p.Status.ToString(), p.DurationWeeks, p.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(new ProgramListResponse(programs, programs.Count));
    }

    private static async Task<IResult> ModifyAthleteSet(
        Guid id, Guid wid, Guid sid,
        CoachModifySetRequest req,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can modify athlete sets.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var rel = await coachService.GetRelationshipAsync(currentUser.UserId, id, ct)
            ?? throw new NotFoundException("CoachAthlete");
        if (!rel.Permissions.CanModifyProgram)
            throw new ForbiddenException("You don't have permission to modify this athlete's program.");

        var set = await db.WorkoutSets
            .Include(s => s.Workout)
            .FirstOrDefaultAsync(s => s.Id == sid && s.WorkoutId == wid && s.Workout!.UserId == id, ct)
            ?? throw new NotFoundException("WorkoutSet", sid.ToString());

        if (set.Workout!.Status == WorkoutStatus.Completed)
            throw new ConflictException("Cannot modify sets on a completed workout.");

        if (req.TargetReps.HasValue) set.TargetReps = req.TargetReps.Value;
        if (req.TargetWeight.HasValue) set.TargetWeight = req.TargetWeight.Value;
        if (req.TargetRpe.HasValue) set.TargetRir = (int)Math.Round(10m - req.TargetRpe.Value);

        set.WasAutoAdjusted = true;
        set.AdjustmentReason = "Modified by coach";

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddNote(
        Guid id,
        CoachNoteRequest req,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can add notes.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var rel = await coachService.GetRelationshipAsync(currentUser.UserId, id, ct)
            ?? throw new NotFoundException("CoachAthlete");
        if (!rel.Permissions.CanAddNotes)
            throw new ForbiddenException("You don't have permission to add notes for this athlete.");

        // Validate context anchors belong to athlete
        if (req.WorkoutId.HasValue)
        {
            var workoutExists = await db.Workouts.AnyAsync(
                w => w.Id == req.WorkoutId.Value && w.UserId == id, ct);
            if (!workoutExists)
                throw new NotFoundException("Workout", req.WorkoutId.Value.ToString());
        }

        if (req.WorkoutSetId.HasValue)
        {
            var setExists = await db.WorkoutSets
                .Include(s => s.Workout)
                .AnyAsync(s => s.Id == req.WorkoutSetId.Value && s.Workout!.UserId == id, ct);
            if (!setExists)
                throw new NotFoundException("WorkoutSet", req.WorkoutSetId.Value.ToString());
        }

        var note = new CoachNote
        {
            Id = Guid.NewGuid(),
            CoachAthleteId = rel.Id,
            CoachId = currentUser.UserId,
            AthleteId = id,
            Content = req.Content,
            WorkoutId = req.WorkoutId,
            WorkoutSetId = req.WorkoutSetId
        };

        _db_Add(db, note);
        await db.SaveChangesAsync(ct);

        var coach = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);

        return Results.Created($"/api/v1/coach/athletes/{id}/notes/{note.Id}",
            ToNoteResponse(note, coach?.DisplayName ?? "Unknown"));
    }

    private static async Task<IResult> ListNotes(
        Guid id,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view notes.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var notes = await db.CoachNotes
            .AsNoTracking()
            .Where(n => n.AthleteId == id && n.CoachId == currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        var coach = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        var coachName = coach?.DisplayName ?? "Unknown";

        var items = notes.Select(n => ToNoteResponse(n, coachName)).ToList();
        return Results.Ok(new CoachNoteListResponse(items, items.Count));
    }

    private static async Task<IResult> DeleteNote(
        Guid id, Guid nid,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can delete notes.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var note = await db.CoachNotes
            .FirstOrDefaultAsync(n => n.Id == nid && n.AthleteId == id && n.CoachId == currentUser.UserId, ct)
            ?? throw new NotFoundException("CoachNote", nid.ToString());

        db.CoachNotes.Remove(note);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static void _db_Add(TrainingAppDbContext db, CoachNote note)
    {
        db.CoachNotes.Add(note);
    }

    private static CoachNoteResponse ToNoteResponse(CoachNote n, string coachName) =>
        new(n.Id, n.CoachId, coachName, n.AthleteId, n.WorkoutId, n.WorkoutSetId, n.Content, n.CreatedAt);
}
