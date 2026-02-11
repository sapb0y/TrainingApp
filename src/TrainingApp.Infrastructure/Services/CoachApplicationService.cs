using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class CoachApplicationService : ICoachApplicationService
{
    private readonly TrainingAppDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEmailService _emailService;

    public CoachApplicationService(
        TrainingAppDbContext db,
        UserManager<User> userManager,
        ISubscriptionService subscriptionService,
        IEmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _subscriptionService = subscriptionService;
        _emailService = emailService;
    }

    public async Task<CoachApplication> SubmitApplicationAsync(
        Guid userId, string credentials, int clientCount,
        string businessGoal, string? additionalInfo, CancellationToken ct = default)
    {
        var existing = await _db.CoachApplications
            .AnyAsync(a => a.UserId == userId
                && a.Status != CoachApplicationStatus.Rejected, ct);

        if (existing)
            throw new ConflictException("You already have a pending or approved coach application");

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Credentials = credentials,
            CurrentClientCount = clientCount,
            BusinessGoal = businessGoal,
            AdditionalInfo = additionalInfo
        };

        _db.CoachApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        return application;
    }

    public async Task<CoachApplication?> GetApplicationAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.CoachApplications
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<CoachApplication>> GetPendingApplicationsAsync(CancellationToken ct = default)
    {
        return await _db.CoachApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.Status == CoachApplicationStatus.Pending)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<CoachApplication> ReviewApplicationAsync(
        Guid applicationId, Guid reviewerId, bool approve, string? notes, CancellationToken ct = default)
    {
        var application = await _db.CoachApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("CoachApplication", applicationId.ToString());

        if (application.Status != CoachApplicationStatus.Pending)
            throw new ConflictException("Application has already been reviewed");

        application.Status = approve ? CoachApplicationStatus.Approved : CoachApplicationStatus.Rejected;
        application.ReviewedById = reviewerId;
        application.ReviewedAt = DateTimeOffset.UtcNow;
        application.ReviewNotes = notes;

        var user = await _userManager.FindByIdAsync(application.UserId.ToString())
            ?? throw new NotFoundException("User", application.UserId.ToString());

        if (approve)
        {
            await _userManager.AddToRoleAsync(user, "Coach");
            await _subscriptionService.ChangeTierAsync(application.UserId, SubscriptionTier.Coach, ct);
            _ = _emailService.SendCoachApprovedAsync(user.Email!, user.DisplayName, ct);
        }
        else
        {
            _ = _emailService.SendCoachRejectedAsync(user.Email!, user.DisplayName, notes, ct);
        }

        await _db.SaveChangesAsync(ct);
        return application;
    }
}
