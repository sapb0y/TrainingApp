using TrainingApp.Core.DTOs;

namespace TrainingApp.Core.Interfaces;

public interface IAdminService
{
    Task<AdminStats> GetSystemStatsAsync(CancellationToken ct = default);
    Task<PagedResult<AdminUserSummary>> GetUsersAsync(string? query, string? role, string? tier, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<AdminUserDetail> GetUserDetailAsync(Guid userId, CancellationToken ct = default);
    Task ChangeUserRoleAsync(Guid adminUserId, Guid userId, string newRole, CancellationToken ct = default);
    Task<PagedResult<AdminSubscriptionSummary>> GetSubscriptionsAsync(string? status, string? tier, int page, int pageSize, CancellationToken ct = default);
    Task OverrideTierAsync(Guid adminUserId, Guid subscriptionId, string tier, string reason, CancellationToken ct = default);
    Task ExtendTrialAsync(Guid adminUserId, Guid subscriptionId, int days, CancellationToken ct = default);
    Task AdminCancelSubscriptionAsync(Guid adminUserId, Guid subscriptionId, string reason, CancellationToken ct = default);
    Task<PagedResult<PaymentFailureSummary>> GetPaymentFailuresAsync(int page, int pageSize, CancellationToken ct = default);
    Task<List<ActivityEntry>> GetRecentActivityAsync(int count, CancellationToken ct = default);
}
