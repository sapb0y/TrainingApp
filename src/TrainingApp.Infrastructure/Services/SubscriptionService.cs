using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly TrainingAppDbContext _db;

    public SubscriptionService(TrainingAppDbContext db)
    {
        _db = db;
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
}
