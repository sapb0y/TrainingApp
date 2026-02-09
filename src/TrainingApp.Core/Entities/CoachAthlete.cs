namespace TrainingApp.Core.Entities;

public class CoachAthlete
{
    public Guid Id { get; set; }
    public Guid CoachId { get; set; }
    public User? Coach { get; set; }
    public Guid? AthleteId { get; set; }
    public User? Athlete { get; set; }
    public required string InviteCode { get; set; }
    public CoachAthleteStatus Status { get; set; } = CoachAthleteStatus.Pending;
    public CoachPermissions Permissions { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public ICollection<CoachNote> Notes { get; set; } = [];
}

public enum CoachAthleteStatus { Pending, Active, Ended }

public class CoachPermissions
{
    public bool CanModifyProgram { get; set; } = true;
    public bool CanViewMetrics { get; set; } = true;
    public bool CanAddNotes { get; set; } = true;
}
