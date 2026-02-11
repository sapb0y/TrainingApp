using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly TrainingAppDbContext _db;
    private readonly StripePriceIds _priceIds;

    public SubscriptionService(TrainingAppDbContext db, IOptions<StripeSettings> stripeSettings)
    {
        _db = db;
        _priceIds = stripeSettings.Value.PriceIds;
    }

    public async Task<UserSubscription> CreateTrialAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (existing is not null)
            throw new ConflictException("User already has a subscription");

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Tier = SubscriptionTier.Competitor,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTimeOffset.UtcNow,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        _db.UserSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        return subscription;
    }

    public async Task<UserSubscription?> GetSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
    }

    public async Task<UserSubscription> ChangeTierAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default)
    {
        var subscription = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new NotFoundException("UserSubscription", userId.ToString());

        subscription.Tier = tier;
        subscription.Status = SubscriptionStatus.Active;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        if (tier == SubscriptionTier.Coach)
        {
            subscription.CoachFreeUntil = DateTimeOffset.UtcNow.AddMonths(3);
        }

        await _db.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task<SubscriptionTier> GetEffectiveTierAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (subscription is null)
            return SubscriptionTier.Athlete;

        if (subscription.Status == SubscriptionStatus.Trial && IsTrialExpired(subscription))
            return SubscriptionTier.Athlete;

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            return SubscriptionTier.Athlete;

        return subscription.Tier;
    }

    public bool IsTrialExpired(UserSubscription subscription)
    {
        return subscription.Status == SubscriptionStatus.Trial
            && subscription.TrialEndDate.HasValue
            && subscription.TrialEndDate.Value < DateTimeOffset.UtcNow;
    }

    public async Task CheckCoachClientThresholdAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Tier == SubscriptionTier.Coach, ct);

        if (subscription is null || subscription.CoachFreeForever)
            return;

        var activeClientCount = await _db.CoachAthletes
            .CountAsync(ca => ca.CoachId == userId && ca.Status == CoachAthleteStatus.Active, ct);

        if (activeClientCount >= 20)
        {
            subscription.CoachFreeForever = true;
            subscription.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ActivateFromPaymentAsync(string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, DateTimeOffset currentPeriodEnd, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, ct)
            ?? throw new NotFoundException("UserSubscription", stripeCustomerId);

        sub.StripeSubscriptionId = stripeSubscriptionId;
        sub.Tier = ResolveTierFromPriceId(stripePriceId);
        sub.Interval = ResolveIntervalFromPriceId(stripePriceId);
        sub.Status = SubscriptionStatus.Active;
        sub.CurrentPeriodEnd = currentPeriodEnd;
        sub.CancelledAt = null;
        sub.PaymentFailedAt = null;
        sub.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetPaymentFailedAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var sub = await GetByStripeSubscriptionIdTrackedAsync(stripeSubscriptionId, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.PastDue;
        sub.PaymentFailedAt = DateTimeOffset.UtcNow;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetCancelledAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var sub = await GetByStripeSubscriptionIdTrackedAsync(stripeSubscriptionId, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.Cancelled;
        sub.CancelledAt = DateTimeOffset.UtcNow;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePeriodAsync(string stripeSubscriptionId, DateTimeOffset currentPeriodEnd, SubscriptionStatus status, CancellationToken ct = default)
    {
        var sub = await GetByStripeSubscriptionIdTrackedAsync(stripeSubscriptionId, ct);
        if (sub is null) return;

        sub.CurrentPeriodEnd = currentPeriodEnd;
        sub.Status = status;
        if (status == SubscriptionStatus.Active)
            sub.PaymentFailedAt = null;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserSubscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default)
    {
        return await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, ct);
    }

    public async Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        return await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
    }

    public async Task<bool> RequiresPaymentAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (sub is null) return true;

        // Coach with free period or free forever doesn't need payment
        if (tier == SubscriptionTier.Coach)
        {
            if (sub.CoachFreeForever) return false;
            if (sub.CoachFreeUntil.HasValue && sub.CoachFreeUntil.Value > DateTimeOffset.UtcNow) return false;
        }

        return true;
    }

    public async Task SetStripeCustomerIdAsync(Guid userId, string stripeCustomerId, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new NotFoundException("UserSubscription", userId.ToString());

        sub.StripeCustomerId = stripeCustomerId;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public int? GetTrialDaysRemaining(UserSubscription subscription)
    {
        if (subscription.Status != SubscriptionStatus.Trial || !subscription.TrialEndDate.HasValue)
            return null;

        var remaining = (subscription.TrialEndDate.Value - DateTimeOffset.UtcNow).Days;
        return remaining < 0 ? 0 : remaining;
    }

    private async Task<UserSubscription?> GetByStripeSubscriptionIdTrackedAsync(string stripeSubscriptionId, CancellationToken ct)
    {
        return await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
    }

    private SubscriptionTier ResolveTierFromPriceId(string priceId)
    {
        if (priceId == _priceIds.AthleteMonthly || priceId == _priceIds.AthleteAnnual)
            return SubscriptionTier.Athlete;
        if (priceId == _priceIds.CompetitorMonthly || priceId == _priceIds.CompetitorAnnual)
            return SubscriptionTier.Competitor;
        if (priceId == _priceIds.CoachMonthly || priceId == _priceIds.CoachAnnual)
            return SubscriptionTier.Coach;

        throw new ArgumentException($"Unknown Stripe price ID: {priceId}");
    }

    private BillingInterval ResolveIntervalFromPriceId(string priceId)
    {
        if (priceId == _priceIds.AthleteAnnual || priceId == _priceIds.CompetitorAnnual || priceId == _priceIds.CoachAnnual)
            return BillingInterval.Annual;
        return BillingInterval.Monthly;
    }
}
