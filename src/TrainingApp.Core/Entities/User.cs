using Microsoft.AspNetCore.Identity;

namespace TrainingApp.Core.Entities;

public class User : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public UserSettings Settings { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Workout> Workouts { get; set; } = [];
    public ICollection<Program> Programs { get; set; } = [];
    public ICollection<DailyMetrics> DailyMetrics { get; set; } = [];
    public ICollection<RecoveryLog> RecoveryLogs { get; set; } = [];
    public ICollection<WeightLog> WeightLogs { get; set; } = [];
    public ICollection<DeficitPhase> DeficitPhases { get; set; } = [];
    public ICollection<NeatLog> NeatLogs { get; set; } = [];
    public ICollection<CardioSession> CardioSessions { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Insight> Insights { get; set; } = [];
    public ICollection<Partnership> PartnershipsAsRequester { get; set; } = [];
    public ICollection<Partnership> PartnershipsAsResponder { get; set; } = [];
    public ICollection<CoachAthlete> CoachRelationshipsAsCoach { get; set; } = [];
    public ICollection<CoachAthlete> CoachRelationshipsAsAthlete { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public class UserSettings
{
    public string WeightUnit { get; set; } = "kg";
    public int DefaultRestSeconds { get; set; } = 90;
    public bool AutoProgressionEnabled { get; set; } = true;
    public decimal RpeTarget { get; set; } = 8.0m;
    public TrainingExperience Experience { get; set; } = TrainingExperience.Intermediate;
    public DateOnly? DateOfBirth { get; set; }
    public RecoveryCapacity RecoveryCapacity { get; set; } = RecoveryCapacity.Normal;
    public BiologicalSex? Sex { get; set; }
    public decimal? HeightCm { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Moderate;
    public WeightGoal WeightGoal { get; set; } = WeightGoal.Maintain;
    public decimal? TargetWeightKg { get; set; }
    public int? MaxHeartRate { get; set; }
    public CardioPlacement CardioPlacement { get; set; } = CardioPlacement.After;
    public int MinSeparationHours { get; set; } = 3;
}

public enum TrainingExperience
{
    Beginner,
    Intermediate,
    Advanced
}

public enum RecoveryCapacity
{
    High,
    Normal,
    Low
}

public enum BiologicalSex { Male, Female }
public enum ActivityLevel { Sedentary, Light, Moderate, Active, VeryActive }
public enum WeightGoal { Lose, Maintain, Gain }
public enum CardioModality { Running, Cycling, Rowing, Swimming, Walking, Elliptical, Other }
public enum CardioIntensityZone { Zone1, Zone2, Zone3, Zone4, Zone5 }
public enum CardioPlacement { Before, After, Separate }
