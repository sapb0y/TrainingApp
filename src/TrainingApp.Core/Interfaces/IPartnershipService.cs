using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IPartnershipService
{
    Task<Partnership> CreateInviteAsync(Guid requesterId, CancellationToken ct);
    Task<Partnership> AcceptInviteAsync(Guid responderId, string inviteCode, CancellationToken ct);
    Task DeclineInviteAsync(Guid userId, string inviteCode, CancellationToken ct);
    Task<Partnership?> GetPartnershipAsync(Guid userId, Guid partnershipId, CancellationToken ct);
    Task<Partnership?> GetActivePartnershipAsync(Guid userId, CancellationToken ct);
    Task<List<Partnership>> GetPartnershipsAsync(Guid userId, CancellationToken ct);
    Task<Partnership> EndPartnershipAsync(Guid userId, Guid partnershipId, CancellationToken ct);
}
