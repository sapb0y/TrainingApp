using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;
using ProgramEntity = TrainingApp.Core.Entities.Program;

namespace TrainingApp.Api.Endpoints;

public static class ProgramEndpoints
{
    public static void MapProgramEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/programs")
            .WithTags("Programs")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateProgram)
            .WithName("GenerateProgram")
            .WithSummary("Generate a new periodized training program")
            .WithValidation<GenerateProgramRequest>();

        group.MapGet("/", GetAll)
            .WithName("GetPrograms")
            .WithSummary("Get all programs for current user");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetProgramById")
            .WithSummary("Get program by ID with full hierarchy");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateProgram")
            .WithSummary("Update program name, description, or status")
            .WithValidation<UpdateProgramRequest>();

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteProgram")
            .WithSummary("Delete a program and all its phases/workouts");
    }

    private static async Task<IResult> GenerateProgram(
        GenerateProgramRequest req,
        ICurrentUserService currentUser,
        IProgramGeneratorService generator,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var goal = Enum.Parse<ProgramGoal>(req.Goal, true);
        var template = Enum.Parse<ProgramTemplate>(req.Template, true);
        var startDate = DateOnly.Parse(req.StartDate);

        var program = await generator.GenerateProgramAsync(
            userId, req.Name, goal, template, req.DurationWeeks, startDate, ct);

        return Results.Created($"/api/v1/programs/{program.Id}", ToResponse(program));
    }

    private static async Task<IResult> GetAll(
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var programs = await db.Programs
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProgramSummaryResponse(
                p.Id,
                p.Name,
                p.Goal.ToString(),
                p.Template.ToString(),
                p.Status.ToString(),
                p.DurationWeeks,
                p.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(new ProgramListResponse(programs, programs.Count));
    }

    private static async Task<IResult> GetById(
        Guid id,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var program = await db.Programs
            .AsNoTracking()
            .Include(p => p.Phases).ThenInclude(ph => ph.VolumeTargets)
            .Include(p => p.Phases).ThenInclude(ph => ph.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

        if (program is null)
            throw new NotFoundException("Program", id.ToString());

        return Results.Ok(ToResponse(program));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateProgramRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var program = await db.Programs
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

        if (program is null)
            throw new NotFoundException("Program", id.ToString());

        if (req.Name is not null) program.Name = req.Name;
        if (req.Description is not null) program.Description = req.Description;
        if (req.Status is not null) program.Status = Enum.Parse<ProgramStatus>(req.Status, true);

        await db.SaveChangesAsync(ct);

        // Reload with hierarchy for response
        var updated = await db.Programs
            .AsNoTracking()
            .Include(p => p.Phases).ThenInclude(ph => ph.VolumeTargets)
            .Include(p => p.Phases).ThenInclude(ph => ph.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise)
            .FirstAsync(p => p.Id == id, ct);

        return Results.Ok(ToResponse(updated));
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var program = await db.Programs
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

        if (program is null)
            throw new NotFoundException("Program", id.ToString());

        db.Programs.Remove(program);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static ProgramResponse ToResponse(ProgramEntity p) => new(
        p.Id,
        p.UserId,
        p.Name,
        p.Description,
        p.Goal.ToString(),
        p.Template.ToString(),
        p.Status.ToString(),
        p.DurationWeeks,
        p.StartDate.ToString("yyyy-MM-dd"),
        p.CreatedAt,
        p.Phases.OrderBy(ph => ph.OrderIndex).Select(ToPhaseResponse).ToList());

    private static ProgramPhaseResponse ToPhaseResponse(ProgramPhase ph) => new(
        ph.Id,
        ph.Name,
        ph.Type.ToString(),
        ph.OrderIndex,
        ph.DurationWeeks,
        ph.VolumeMultiplier,
        ph.IntensityMultiplier,
        ph.TargetRir,
        ph.VolumeTargets.OrderBy(v => v.MuscleGroup).Select(ToVolumeTargetResponse).ToList(),
        ph.Workouts.OrderBy(w => w.OrderIndex).Select(ToWorkoutResponse).ToList());

    private static ProgramWorkoutResponse ToWorkoutResponse(ProgramWorkout w) => new(
        w.Id,
        w.Name,
        w.Focus.ToString(),
        w.DayNumber,
        w.OrderIndex,
        w.Exercises.OrderBy(e => e.OrderIndex).Select(ToExerciseResponse).ToList());

    private static ProgramExerciseResponse ToExerciseResponse(ProgramExercise e) => new(
        e.Id,
        e.ExerciseId,
        e.Exercise?.Name ?? "Unknown",
        e.OrderIndex,
        e.TargetSets,
        e.TargetRepsMin,
        e.TargetRepsMax,
        e.IntensityPercentage,
        e.TargetRpe,
        e.RestSeconds,
        e.Notes);

    private static MuscleVolumeTargetResponse ToVolumeTargetResponse(MuscleVolumeTarget v) => new(
        v.Id,
        v.MuscleGroup,
        v.MinSetsPerWeek,
        v.TargetSetsPerWeek,
        v.MaxSetsPerWeek);
}
