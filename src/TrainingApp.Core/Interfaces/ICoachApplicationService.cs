using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface ICoachApplicationService
{
    Task<CoachApplication> SubmitApplicationAsync(Guid userId, string credentials,
        int clientCount, string businessGoal, string? additionalInfo, CancellationToken ct = default);
    Task<CoachApplication?> GetApplicationAsync(Guid userId, CancellationToken ct = default);
    Task<List<CoachApplication>> GetPendingApplicationsAsync(CancellationToken ct = default);
    Task<CoachApplication> ReviewApplicationAsync(Guid applicationId, Guid reviewerId,
        bool approve, string? notes, CancellationToken ct = default);
}
