using TrainingApp.Core.Services;

namespace TrainingApp.Core.Interfaces;

public interface IUserDashboardService
{
    Task<UserDashboardData> GetDashboardAsync(Guid userId, CancellationToken ct = default);
}

public record UserDashboardData(
    ChartDataService.DashboardSummary Summary,
    int ActiveDays,
    List<UserDashboardPR> RecentPRs);

public record UserDashboardPR(string ExerciseName, decimal BestE1rm, DateOnly Date);
