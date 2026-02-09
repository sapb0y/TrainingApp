using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface ICoachAthleteService
{
    Task<CoachAthlete> CreateInviteAsync(Guid coachId, CancellationToken ct);
    Task<CoachAthlete> AcceptInviteAsync(Guid athleteId, string inviteCode, CancellationToken ct);
    Task<CoachAthlete?> GetRelationshipAsync(Guid coachId, Guid athleteId, CancellationToken ct);
    Task<List<CoachAthlete>> GetAthletesAsync(Guid coachId, CancellationToken ct);
    Task<CoachAthlete> EndRelationshipAsync(Guid coachId, Guid athleteId, CancellationToken ct);
    Task<CoachAthlete> UpdatePermissionsAsync(Guid coachId, Guid athleteId, CoachPermissions perms, CancellationToken ct);
    Task ValidateCoachAccessAsync(Guid coachId, Guid athleteId, CancellationToken ct);
}
