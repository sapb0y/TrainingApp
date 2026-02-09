using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class PartnershipService : IPartnershipService
{
    private readonly TrainingAppDbContext _db;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public PartnershipService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<Partnership> CreateInviteAsync(Guid requesterId, CancellationToken ct)
    {
        var hasActive = await _db.Partnerships.AnyAsync(
            p => (p.RequesterId == requesterId || p.ResponderId == requesterId)
                 && p.Status == PartnershipStatus.Active, ct);
        if (hasActive)
            throw new ConflictException("You already have an active partnership.");

        var code = GenerateCode();
        while (await _db.Partnerships.AnyAsync(p => p.InviteCode == code, ct))
            code = GenerateCode();

        var partnership = new Partnership
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            InviteCode = code,
            Status = PartnershipStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        _db.Partnerships.Add(partnership);
        await _db.SaveChangesAsync(ct);

        // Load requester nav prop
        await _db.Entry(partnership).Reference(p => p.Requester).LoadAsync(ct);
        return partnership;
    }

    public async Task<Partnership> AcceptInviteAsync(Guid responderId, string inviteCode, CancellationToken ct)
    {
        var partnership = await _db.Partnerships
            .Include(p => p.Requester)
            .FirstOrDefaultAsync(p => p.InviteCode == inviteCode.ToUpperInvariant() && p.Status == PartnershipStatus.Pending, ct)
            ?? throw new NotFoundException("Partnership");

        if (partnership.ExpiresAt.HasValue && partnership.ExpiresAt.Value < DateTimeOffset.UtcNow)
            throw new ConflictException("Invite has expired.");

        if (partnership.RequesterId == responderId)
            throw new ValidationException("InviteCode", "You cannot accept your own invite.");

        var hasActive = await _db.Partnerships.AnyAsync(
            p => (p.RequesterId == responderId || p.ResponderId == responderId)
                 && p.Status == PartnershipStatus.Active, ct);
        if (hasActive)
            throw new ConflictException("You already have an active partnership.");

        partnership.ResponderId = responderId;
        partnership.Status = PartnershipStatus.Active;
        partnership.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(partnership).Reference(p => p.Responder).LoadAsync(ct);
        return partnership;
    }

    public async Task DeclineInviteAsync(Guid userId, string inviteCode, CancellationToken ct)
    {
        var partnership = await _db.Partnerships
            .FirstOrDefaultAsync(p => p.InviteCode == inviteCode.ToUpperInvariant() && p.Status == PartnershipStatus.Pending, ct)
            ?? throw new NotFoundException("Partnership");

        partnership.Status = PartnershipStatus.Ended;
        partnership.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Partnership?> GetPartnershipAsync(Guid userId, Guid partnershipId, CancellationToken ct)
    {
        return await _db.Partnerships
            .AsNoTracking()
            .Include(p => p.Requester)
            .Include(p => p.Responder)
            .FirstOrDefaultAsync(p => p.Id == partnershipId
                && (p.RequesterId == userId || p.ResponderId == userId), ct);
    }

    public async Task<Partnership?> GetActivePartnershipAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Partnerships
            .AsNoTracking()
            .Include(p => p.Requester)
            .Include(p => p.Responder)
            .FirstOrDefaultAsync(p => (p.RequesterId == userId || p.ResponderId == userId)
                && p.Status == PartnershipStatus.Active, ct);
    }

    public async Task<List<Partnership>> GetPartnershipsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Partnerships
            .AsNoTracking()
            .Include(p => p.Requester)
            .Include(p => p.Responder)
            .Where(p => p.RequesterId == userId || p.ResponderId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Partnership> EndPartnershipAsync(Guid userId, Guid partnershipId, CancellationToken ct)
    {
        var partnership = await _db.Partnerships
            .Include(p => p.Requester)
            .Include(p => p.Responder)
            .Include(p => p.SharedSessions)
            .FirstOrDefaultAsync(p => p.Id == partnershipId
                && (p.RequesterId == userId || p.ResponderId == userId), ct)
            ?? throw new NotFoundException("Partnership");

        if (partnership.Status != PartnershipStatus.Active)
            throw new ConflictException("Partnership is not active.");

        partnership.Status = PartnershipStatus.Ended;
        partnership.UpdatedAt = DateTimeOffset.UtcNow;

        // Cancel any planned shared sessions
        foreach (var session in partnership.SharedSessions.Where(s => s.Status == SharedSessionStatus.Planned))
            session.Status = SharedSessionStatus.Cancelled;

        await _db.SaveChangesAsync(ct);
        return partnership;
    }

    private static string GenerateCode()
    {
        return new string(Enumerable.Range(0, 6)
            .Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)])
            .ToArray());
    }
}
