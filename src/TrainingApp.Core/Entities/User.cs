using Microsoft.AspNetCore.Identity;

namespace TrainingApp.Core.Entities;

public class User : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public UserSettings Settings { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Workout> Workouts { get; set; } = [];
}

public class UserSettings
{
    public string WeightUnit { get; set; } = "kg";
    public int DefaultRestSeconds { get; set; } = 90;
    public bool AutoProgressionEnabled { get; set; } = true;
    public decimal RpeTarget { get; set; } = 8.0m;
}
