namespace TrainingApp.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportWorkoutsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<byte[]> ExportWeightLogsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<byte[]> ExportCardioSessionsCsvAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct);
}
