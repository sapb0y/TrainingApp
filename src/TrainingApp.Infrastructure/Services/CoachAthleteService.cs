using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class CoachAthleteService : ICoachAthleteService
{
    private readonly TrainingAppDbContext _db;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public CoachAthleteService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<CoachAthlete> CreateInviteAsync(Guid coachId, CancellationToken ct)
    {
        var code = GenerateCode();
        while (await _db.CoachAthletes.AnyAsync(c => c.InviteCode == code, ct))
            code = GenerateCode();

        var relationship = new CoachAthlete
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            InviteCode = code,
            Status = CoachAthleteStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        _db.CoachAthletes.Add(relationship);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(relationship).Reference(r => r.Coach).LoadAsync(ct);
        return relationship;
    }

    public async Task<CoachAthlete> AcceptInviteAsync(Guid athleteId, string inviteCode, CancellationToken ct)
    {
        var relationship = await _db.CoachAthletes
            .Include(c => c.Coach)
            .FirstOrDefaultAsync(c => c.InviteCode == inviteCode.ToUpperInvariant()
                && c.Status == CoachAthleteStatus.Pending, ct)
            ?? throw new NotFoundException("CoachInvite", inviteCode);

        if (relationship.ExpiresAt.HasValue && relationship.ExpiresAt.Value < DateTimeOffset.UtcNow)
            throw new ConflictException("Invite has expired.");

        if (relationship.CoachId == athleteId)
            throw new ConflictException("You cannot accept your own invite.");

        var hasActiveCoach = await _db.CoachAthletes.AnyAsync(
            c => c.AthleteId == athleteId && c.Status == CoachAthleteStatus.Active, ct);
        if (hasActiveCoach)
            throw new ConflictException("You already have an active coach.");

        relationship.AthleteId = athleteId;
        relationship.Status = CoachAthleteStatus.Active;
        relationship.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _db.Entry(relationship).Reference(r => r.Athlete).LoadAsync(ct);
        return relationship;
    }

    public async Task<CoachAthlete?> GetRelationshipAsync(Guid coachId, Guid athleteId, CancellationToken ct)
    {
        return await _db.CoachAthletes
            .AsNoTracking()
            .Include(c => c.Coach)
            .Include(c => c.Athlete)
            .FirstOrDefaultAsync(c => c.CoachId == coachId && c.AthleteId == athleteId
                && c.Status == CoachAthleteStatus.Active, ct);
    }

    public async Task<List<CoachAthlete>> GetAthletesAsync(Guid coachId, CancellationToken ct)
    {
        return await _db.CoachAthletes
            .AsNoTracking()
            .Include(c => c.Coach)
            .Include(c => c.Athlete)
            .Where(c => c.CoachId == coachId && c.Status == CoachAthleteStatus.Active)
            .OrderBy(c => c.Athlete!.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<CoachAthlete> EndRelationshipAsync(Guid coachId, Guid athleteId, CancellationToken ct)
    {
        var relationship = await _db.CoachAthletes
            .Include(c => c.Coach)
            .Include(c => c.Athlete)
            .FirstOrDefaultAsync(c => c.CoachId == coachId && c.AthleteId == athleteId
                && c.Status == CoachAthleteStatus.Active, ct)
            ?? throw new NotFoundException("CoachAthlete");

        relationship.Status = CoachAthleteStatus.Ended;
        relationship.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return relationship;
    }

    public async Task<CoachAthlete> UpdatePermissionsAsync(Guid coachId, Guid athleteId, CoachPermissions perms, CancellationToken ct)
    {
        var relationship = await _db.CoachAthletes
            .Include(c => c.Coach)
            .Include(c => c.Athlete)
            .FirstOrDefaultAsync(c => c.CoachId == coachId && c.AthleteId == athleteId
                && c.Status == CoachAthleteStatus.Active, ct)
            ?? throw new NotFoundException("CoachAthlete");

        relationship.Permissions = perms;
        relationship.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Entry(relationship).Property(r => r.Permissions).IsModified = true;
        await _db.SaveChangesAsync(ct);
        return relationship;
    }

    public async Task ValidateCoachAccessAsync(Guid coachId, Guid athleteId, CancellationToken ct)
    {
        var hasAccess = await _db.CoachAthletes.AnyAsync(
            c => c.CoachId == coachId && c.AthleteId == athleteId
                && c.Status == CoachAthleteStatus.Active, ct);
        if (!hasAccess)
            throw new ForbiddenException("You do not have an active coaching relationship with this athlete.");
    }

    private static string GenerateCode()
    {
        return new string(Enumerable.Range(0, 6)
            .Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)])
            .ToArray());
    }
}
