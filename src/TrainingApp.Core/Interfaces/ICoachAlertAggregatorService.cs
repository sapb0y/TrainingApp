using TrainingApp.Core.Services;

namespace TrainingApp.Core.Interfaces;

public interface ICoachAlertAggregatorService
{
    Task<List<CoachAlertEngine.CoachAlert>> GetAlertsForAllAthletesAsync(Guid coachId, CancellationToken ct);
    Task<List<CoachAlertEngine.CoachAlert>> GetAlertsForAthleteAsync(Guid coachId, Guid athleteId, CancellationToken ct);
}
