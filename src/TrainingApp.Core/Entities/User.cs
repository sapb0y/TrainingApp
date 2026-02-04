namespace TrainingApp.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public UserSettings Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Workout> Workouts { get; set; } = [];
}

public class UserSettings
{
    public string WeightUnit { get; set; } = "kg";
    public int DefaultRestSeconds { get; set; } = 90;
    public bool AutoProgressionEnabled { get; set; } = true;
    public decimal RpeTarget { get; set; } = 8.0m;
}
