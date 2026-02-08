using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface ISharedSessionService
{
    Task<SharedSession> CreateSessionAsync(Guid userId, Guid partnershipId,
        DateOnly date, Guid? workoutAId, Guid? workoutBId, string? notes, CancellationToken ct);
    Task<SharedSession?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct);
    Task<List<SharedSession>> GetSessionsAsync(Guid userId, Guid partnershipId, CancellationToken ct);
    Task<SharedSession> GenerateScheduleAsync(Guid userId, Guid sessionId, CancellationToken ct);
    Task<SharedSession> CancelSessionAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
