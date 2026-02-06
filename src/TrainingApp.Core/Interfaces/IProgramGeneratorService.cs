using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IProgramGeneratorService
{
    Task<Program> GenerateProgramAsync(
        Guid userId,
        string name,
        ProgramGoal goal,
        ProgramTemplate template,
        int durationWeeks,
        DateOnly startDate,
        CancellationToken ct = default);
}
