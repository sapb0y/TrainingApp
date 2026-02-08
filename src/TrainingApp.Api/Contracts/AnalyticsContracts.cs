namespace TrainingApp.Api.Contracts;

// Chart responses
public record ChartDataResponse(ChartSeriesResponse Series, ChartStatsResponse Statistics);
public record MultiSeriesChartResponse(List<ChartSeriesResponse> Series, string Title);
public record ChartSeriesResponse(string Name, string Unit, List<ChartPointResponse> Points);
public record ChartPointResponse(string Date, decimal Value, string? Label);
public record ChartStatsResponse(decimal Min, decimal Max, decimal Average, decimal? Current, decimal? ChangePercent);

// Dashboard
public record DashboardResponse(
    StrengthSnapshotResponse Strength, BodyWeightSnapshotResponse BodyWeight,
    CardioSnapshotResponse Cardio, FatigueSnapshotResponse Fatigue,
    VolumeSnapshotResponse Volume, int ActiveDaysLast30,
    List<PersonalRecordResponse> RecentPRs);
public record StrengthSnapshotResponse(int ExercisesTracked, decimal? TopE1rm, string? TopExercise, decimal? E1rmChangePct30d);
public record BodyWeightSnapshotResponse(decimal? CurrentKg, decimal? MovingAvg7d, decimal? WeeklyRateKg, string? Trend);
public record CardioSnapshotResponse(int SessionsLast30d, int MinutesLast30d, decimal? TotalDistanceKm30d, decimal? AvgTrimp);
public record FatigueSnapshotResponse(decimal? Ctl, decimal? Atl, decimal? Tsb, string? ReadinessCategory);
public record VolumeSnapshotResponse(int TotalSetsLast7d, decimal? TotalVolumeLast7d, decimal? VolumeChangePctVsPrev7d);
public record PersonalRecordResponse(string ExerciseName, decimal BestE1rm, string Date);

// Insights
public record InsightResponse(Guid Id, string RuleName, string Category, string Severity,
    string Title, string Message, string GeneratedDate, string? RelatedEntityType, Guid? RelatedEntityId);
public record InsightListResponse(List<InsightResponse> Items, int TotalCount);
