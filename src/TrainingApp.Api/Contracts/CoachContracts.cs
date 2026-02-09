namespace TrainingApp.Api.Contracts;

public record CoachInviteResponse(Guid RelationshipId, string InviteCode, string ExpiresAt);
public record AcceptCoachInviteRequest(string InviteCode);
public record CoachAthleteResponse(Guid Id, Guid CoachId, string CoachName,
    Guid? AthleteId, string? AthleteName, string Status,
    CoachPermissionsResponse Permissions, string? InviteCode, string? ExpiresAt, DateTimeOffset CreatedAt);
public record CoachPermissionsResponse(bool CanModifyProgram, bool CanViewMetrics, bool CanAddNotes);
public record UpdatePermissionsRequest(bool? CanModifyProgram, bool? CanViewMetrics, bool? CanAddNotes);
public record CoachAthleteListResponse(List<CoachAthleteResponse> Items, int TotalCount);

// Dashboard
public record CoachDashboardResponse(List<CoachAthleteRosterItem> Roster,
    List<CoachAlertResponse> Alerts, int TotalAthletes, int AthletesNeedingAttention);
public record CoachAthleteRosterItem(Guid AthleteId, string AthleteName,
    decimal? Tsb, string? ReadinessCategory, int AlertCount, string? LastWorkoutDate);
public record CoachAlertResponse(Guid AthleteId, string AthleteName,
    string Category, string Severity, string Message, Dictionary<string, object>? Data);
public record CoachAthleteDashboardResponse(Guid AthleteId, string AthleteName,
    FatigueSnapshotResponse? Fatigue, VolumeSnapshotResponse? Volume,
    StrengthSnapshotResponse? Strength, List<WorkoutSummaryResponse> RecentWorkouts,
    List<CoachAlertResponse> Alerts, string? ActiveDeficitStatus,
    List<GoalSummaryResponse> Goals);
public record GoalSummaryResponse(Guid Id, string Title, string Type, string Status,
    decimal? TargetValue, decimal? CurrentValue, string? TargetDate);

// Actions
public record CoachNoteRequest(string Content, Guid? WorkoutId, Guid? WorkoutSetId);
public record CoachNoteResponse(Guid Id, Guid CoachId, string CoachName,
    Guid AthleteId, Guid? WorkoutId, Guid? WorkoutSetId,
    string Content, DateTimeOffset CreatedAt);
public record CoachNoteListResponse(List<CoachNoteResponse> Items, int TotalCount);
public record CoachModifySetRequest(int? TargetReps, decimal? TargetWeight, decimal? TargetRpe);
