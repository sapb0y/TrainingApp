using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.DTOs;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly TrainingAppDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IPaymentService _paymentService;

    // Fixed tier prices for MRR estimate
    private static readonly Dictionary<SubscriptionTier, decimal> TierPrices = new()
    {
        [SubscriptionTier.Athlete] = 20m,
        [SubscriptionTier.Competitor] = 45m,
        [SubscriptionTier.Coach] = 99m
    };

    public AdminService(
        TrainingAppDbContext db,
        UserManager<User> userManager,
        IPaymentService paymentService)
    {
        _db = db;
        _userManager = userManager;
        _paymentService = paymentService;
    }

    public async Task<AdminStats> GetSystemStatsAsync(CancellationToken ct = default)
    {
        var totalUsers = await _db.Users.CountAsync(ct);

        var subStats = await _db.UserSubscriptions
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeCount = subStats.Where(s => s.Status == SubscriptionStatus.Active).Sum(s => s.Count);
        var trialCount = subStats.Where(s => s.Status == SubscriptionStatus.Trial).Sum(s => s.Count);
        var pastDueCount = subStats.Where(s => s.Status == SubscriptionStatus.PastDue).Sum(s => s.Count);
        var cancelledCount = subStats.Where(s => s.Status == SubscriptionStatus.Cancelled).Sum(s => s.Count);

        var coachCount = await _db.UserSubscriptions
            .CountAsync(s => s.Tier == SubscriptionTier.Coach && s.Status == SubscriptionStatus.Active, ct);

        // MRR: count active subs by tier × price
        var mrrData = await _db.UserSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .GroupBy(s => s.Tier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var mrr = mrrData.Sum(d => d.Count * TierPrices.GetValueOrDefault(d.Tier, 0m));

        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var recentRegistrations = await _db.Users
            .CountAsync(u => u.CreatedAt > thirtyDaysAgo, ct);

        return new AdminStats(
            totalUsers, activeCount + trialCount, trialCount, activeCount,
            pastDueCount, cancelledCount, coachCount, mrr, recentRegistrations);
    }

    public async Task<PagedResult<AdminUserSummary>> GetUsersAsync(
        string? query, string? role, string? tier, string? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Users
            .AsNoTracking()
            .Include(u => u.Subscription)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.ToLower();
            q = q.Where(u => u.Email!.ToLower().Contains(search)
                || u.DisplayName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<SubscriptionTier>(tier, true, out var parsedTier))
            q = q.Where(u => u.Subscription != null && u.Subscription.Tier == parsedTier);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubscriptionStatus>(status, true, out var parsedStatus))
            q = q.Where(u => u.Subscription != null && u.Subscription.Status == parsedStatus);

        var totalCount = await q.CountAsync(ct);

        var users = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserSummary(
                u.Id, u.Email, u.DisplayName, null,
                u.Subscription != null ? u.Subscription.Tier.ToString() : null,
                u.Subscription != null ? u.Subscription.Status.ToString() : null,
                u.CreatedAt))
            .ToListAsync(ct);

        // Resolve roles if role filter specified
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            var roleUserIds = roleUsers.Select(u => u.Id).ToHashSet();
            users = users.Where(u => roleUserIds.Contains(u.Id)).ToList();
            totalCount = users.Count;
        }

        return new PagedResult<AdminUserSummary>(users, totalCount, page, pageSize);
    }

    public async Task<AdminUserDetail> GetUserDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId.ToString());

        var workoutCount = await _db.Workouts
            .CountAsync(w => w.UserId == userId, ct);

        var lastWorkout = await _db.Workouts
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => (DateTimeOffset?)w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var coachAthleteCount = await _db.CoachAthletes
            .CountAsync(ca => ca.CoachId == userId && ca.Status == CoachAthleteStatus.Active, ct);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        var sub = user.Subscription;
        return new AdminUserDetail(
            user.Id, user.Email, user.DisplayName, role,
            sub?.Tier.ToString(), sub?.Status.ToString(),
            sub?.TrialEndDate, sub?.CurrentPeriodEnd,
            sub?.StripeCustomerId, sub?.CancelledAt, sub?.PaymentFailedAt,
            workoutCount, lastWorkout, coachAthleteCount, user.CreatedAt);
    }

    public async Task ChangeUserRoleAsync(Guid adminUserId, Guid userId, string newRole, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId.ToString());

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        await _userManager.AddToRoleAsync(user, newRole);

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = AdminAction.RoleChange,
            TargetUserId = userId,
            Detail = $"Role changed to {newRole}"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AdminSubscriptionSummary>> GetSubscriptionsAsync(
        string? status, string? tier, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubscriptionStatus>(status, true, out var parsedStatus))
            q = q.Where(s => s.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<SubscriptionTier>(tier, true, out var parsedTier))
            q = q.Where(s => s.Tier == parsedTier);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSubscriptionSummary(
                s.Id, s.UserId, s.User!.Email, s.User.DisplayName,
                s.Tier.ToString(), s.Status.ToString(), s.Interval.ToString(),
                s.TrialEndDate, s.CurrentPeriodEnd,
                s.StripeCustomerId, s.CancelledAt, s.PaymentFailedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminSubscriptionSummary>(items, totalCount, page, pageSize);
    }

    public async Task OverrideTierAsync(Guid adminUserId, Guid subscriptionId, string tier, string reason, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new NotFoundException("UserSubscription", subscriptionId.ToString());

        if (!Enum.TryParse<SubscriptionTier>(tier, true, out var parsedTier))
            throw new ValidationException("Tier", $"Invalid tier: {tier}");

        sub.Tier = parsedTier;
        sub.Status = SubscriptionStatus.Active;
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = AdminAction.TierOverride,
            TargetUserId = sub.UserId,
            Detail = $"Tier overridden to {tier}: {reason}"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task ExtendTrialAsync(Guid adminUserId, Guid subscriptionId, int days, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new NotFoundException("UserSubscription", subscriptionId.ToString());

        if (sub.Status != SubscriptionStatus.Trial)
            throw new ValidationException("Status", "Can only extend trial subscriptions");

        sub.TrialEndDate = (sub.TrialEndDate ?? DateTimeOffset.UtcNow).AddDays(days);
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = AdminAction.TrialExtend,
            TargetUserId = sub.UserId,
            Detail = $"Trial extended by {days} days"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task AdminCancelSubscriptionAsync(Guid adminUserId, Guid subscriptionId, string reason, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new NotFoundException("UserSubscription", subscriptionId.ToString());

        sub.Status = SubscriptionStatus.Cancelled;
        sub.CancelledAt = DateTimeOffset.UtcNow;
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(sub.StripeSubscriptionId))
            await _paymentService.CancelSubscriptionAsync(sub.StripeSubscriptionId, ct);

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = AdminAction.SubscriptionCancel,
            TargetUserId = sub.UserId,
            Detail = $"Subscription cancelled: {reason}"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<PaymentFailureSummary>> GetPaymentFailuresAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.PaymentFailedAt != null);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(s => s.PaymentFailedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new PaymentFailureSummary(
                s.UserId, s.User!.Email, s.User.DisplayName,
                s.Tier.ToString(), s.PaymentFailedAt, s.StripeCustomerId))
            .ToListAsync(ct);

        return new PagedResult<PaymentFailureSummary>(items, totalCount, page, pageSize);
    }

    public async Task<List<ActivityEntry>> GetRecentActivityAsync(int count, CancellationToken ct = default)
    {
        var registrations = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .Select(u => new ActivityEntry(
                ActivityType.Registration, u.Id,
                $"{u.DisplayName} registered", u.CreatedAt))
            .ToListAsync(ct);

        var subChanges = await _db.UserSubscriptions
            .AsNoTracking()
            .Include(s => s.User)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(count)
            .Select(s => new ActivityEntry(
                ActivityType.SubscriptionChange, s.UserId,
                $"{s.User!.DisplayName} subscription: {s.Status}", s.UpdatedAt))
            .ToListAsync(ct);

        var coachApps = await _db.CoachApplications
            .AsNoTracking()
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .Select(a => new ActivityEntry(
                ActivityType.CoachApplication, a.UserId,
                $"{a.User!.DisplayName} coach application: {a.Status}", a.CreatedAt))
            .ToListAsync(ct);

        var auditLogs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .Select(a => new ActivityEntry(
                ActivityType.AdminAction, a.TargetUserId,
                $"Admin action: {a.Action} - {a.Detail}", a.CreatedAt))
            .ToListAsync(ct);

        return registrations
            .Concat(subChanges)
            .Concat(coachApps)
            .Concat(auditLogs)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }
}
