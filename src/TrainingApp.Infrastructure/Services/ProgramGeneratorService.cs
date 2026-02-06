using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class ProgramGeneratorService : IProgramGeneratorService
{
    private readonly TrainingAppDbContext _db;
    private readonly IExerciseCacheService _exerciseCache;
    private readonly ILogger<ProgramGeneratorService> _logger;

    public ProgramGeneratorService(
        TrainingAppDbContext db,
        IExerciseCacheService exerciseCache,
        ILogger<ProgramGeneratorService> logger)
    {
        _db = db;
        _exerciseCache = exerciseCache;
        _logger = logger;
    }

    public async Task<Program> GenerateProgramAsync(
        Guid userId,
        string name,
        ProgramGoal goal,
        ProgramTemplate template,
        int durationWeeks,
        DateOnly startDate,
        CancellationToken ct = default)
    {
        // 1. Validate exercises exist
        var allExercises = await _exerciseCache.GetAllExercisesAsync(ct);
        if (allExercises.Count == 0)
        {
            throw new Core.Exceptions.ValidationException(
                "Exercises", "No exercises found. Please sync exercises first.");
        }

        // 2. Get user settings for mesocycle calculation
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new Core.Exceptions.NotFoundException("User", userId.ToString());

        var settings = user.Settings;
        int? age = settings.DateOfBirth.HasValue
            ? CalculateAge(settings.DateOfBirth.Value)
            : null;

        // 3. Get template definition
        var templateDef = ProgramTemplates.GetTemplate(template);

        // 4. Calculate mesocycle structure
        var accumulationWeeks = MesocycleCalculatorService.CalculateAccumulationWeeks(
            settings.Experience, age, settings.RecoveryCapacity);
        var phaseStructure = MesocycleCalculatorService.GeneratePhaseStructure(durationWeeks, accumulationWeeks);

        _logger.LogInformation(
            "Generating {Template} program: {Phases} phases, {AccumWeeks}wk accumulation blocks",
            template, phaseStructure.Count, accumulationWeeks);

        // 5. Build program entity
        var program = new Program
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Goal = goal,
            Template = template,
            Status = ProgramStatus.Draft,
            DurationWeeks = durationWeeks,
            StartDate = startDate,
        };

        // 6. Build exercise lookup cache (muscle → exercises)
        var exerciseLookup = BuildExerciseLookup(allExercises);

        // 7. Generate phases
        int phaseIndex = 0;
        foreach (var (phaseType, weeks) in phaseStructure)
        {
            var phase = CreatePhase(program, phaseType, weeks, phaseIndex, goal, templateDef, exerciseLookup);
            program.Phases.Add(phase);
            phaseIndex++;
        }

        // 8. Persist
        _db.Programs.Add(program);
        await _db.SaveChangesAsync(ct);

        // 9. Reload with full include chain
        var result = await _db.Programs
            .AsNoTracking()
            .Include(p => p.Phases).ThenInclude(ph => ph.VolumeTargets)
            .Include(p => p.Phases).ThenInclude(ph => ph.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise)
            .FirstAsync(p => p.Id == program.Id, ct);

        return result;
    }

    private static ProgramPhase CreatePhase(
        Program program,
        PhaseType phaseType,
        int weeks,
        int orderIndex,
        ProgramGoal goal,
        ProgramTemplates.TemplateDefinition templateDef,
        Dictionary<string, List<Exercise>> exerciseLookup)
    {
        bool isDeload = phaseType == PhaseType.Deload;

        var phase = new ProgramPhase
        {
            Id = Guid.NewGuid(),
            ProgramId = program.Id,
            Name = isDeload ? $"Deload {orderIndex / 2 + 1}" : $"Block {orderIndex / 2 + 1}",
            Type = phaseType,
            OrderIndex = orderIndex,
            DurationWeeks = weeks,
            VolumeMultiplier = isDeload ? 0.5m : 1.0m,
            IntensityMultiplier = isDeload ? 0.7m : 1.0m,
            TargetRir = isDeload ? 4 : (goal == ProgramGoal.Strength ? 2 : 3),
        };

        // Volume targets for relevant muscles
        var allMuscles = templateDef.Days
            .SelectMany(d => d.MuscleGroups)
            .Distinct()
            .ToList();

        foreach (var muscle in allMuscles)
        {
            var (mev, mav, mrv) = VolumeCalculatorService.GetVolumeLandmarks(muscle);
            var targetVolume = VolumeCalculatorService.CalculateWeeklyVolume(muscle, goal);

            phase.VolumeTargets.Add(new MuscleVolumeTarget
            {
                Id = Guid.NewGuid(),
                ProgramPhaseId = phase.Id,
                MuscleGroup = muscle,
                MinSetsPerWeek = mev,
                TargetSetsPerWeek = targetVolume,
                MaxSetsPerWeek = mrv,
            });
        }

        // Workouts for each day in template
        var (repMin, repMax) = LoadPrescriptionService.GetRepRange(goal);

        for (int dayIdx = 0; dayIdx < templateDef.Days.Count; dayIdx++)
        {
            var dayTemplate = templateDef.Days[dayIdx];
            var workout = new ProgramWorkout
            {
                Id = Guid.NewGuid(),
                ProgramPhaseId = phase.Id,
                Name = dayTemplate.Name,
                Focus = dayTemplate.Focus,
                DayNumber = dayIdx + 1,
                OrderIndex = dayIdx,
            };

            // Calculate frequency for this muscle in this template
            var muscleFrequency = new Dictionary<string, int>();
            foreach (var day in templateDef.Days)
            {
                foreach (var mg in day.MuscleGroups)
                {
                    muscleFrequency[mg] = muscleFrequency.GetValueOrDefault(mg) + 1;
                }
            }

            int exerciseOrder = 0;
            foreach (var muscleGroup in dayTemplate.MuscleGroups)
            {
                var exercise = FindExercise(exerciseLookup, muscleGroup);
                if (exercise is null) continue;

                var targetVolume = VolumeCalculatorService.CalculateWeeklyVolume(muscleGroup, goal);
                var frequency = muscleFrequency.GetValueOrDefault(muscleGroup, 1);
                var setsPerSession = Math.Max(2, (int)Math.Ceiling((double)targetVolume / frequency));

                workout.Exercises.Add(new ProgramExercise
                {
                    Id = Guid.NewGuid(),
                    ProgramWorkoutId = workout.Id,
                    ExerciseId = exercise.Id,
                    OrderIndex = exerciseOrder++,
                    TargetSets = setsPerSession,
                    TargetRepsMin = repMin,
                    TargetRepsMax = repMax,
                    IntensityPercentage = LoadPrescriptionService.EstimatePercentageForReps((repMin + repMax) / 2),
                    TargetRpe = isDeload ? 5.0m : (goal == ProgramGoal.Strength ? 8.0m : 7.5m),
                    RestSeconds = goal == ProgramGoal.Strength ? 180 : 120,
                });
            }

            phase.Workouts.Add(workout);
        }

        return phase;
    }

    private static Dictionary<string, List<Exercise>> BuildExerciseLookup(IReadOnlyList<Exercise> exercises)
    {
        var lookup = new Dictionary<string, List<Exercise>>();

        foreach (var muscleGroup in MuscleExerciseMapping.AllMuscleGroups)
        {
            var searchTerms = MuscleExerciseMapping.GetSearchTerms(muscleGroup);
            var matched = exercises
                .Where(e => searchTerms.Any(term =>
                    e.PrimaryMuscles.Any(m => m.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Category.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            lookup[muscleGroup] = matched;
        }

        return lookup;
    }

    private static Exercise? FindExercise(Dictionary<string, List<Exercise>> lookup, string muscleGroup)
    {
        if (lookup.TryGetValue(muscleGroup, out var exercises) && exercises.Count > 0)
            return exercises[0];

        return null;
    }

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }
}
