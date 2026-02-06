using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Contracts;

public record WorkoutResponse(
    Guid Id,
    Guid UserId,
    string Name,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Notes,
    string Status,
    int? SessionRpe,
    int? PreWorkoutReadiness,
    int? PostWorkoutFatigue,
    DateTimeOffset CreatedAt,
    List<WorkoutSetResponse> Sets);

public record WorkoutListResponse(
    List<WorkoutSummaryResponse> Items,
    int TotalCount);

public record WorkoutSummaryResponse(
    Guid Id,
    string Name,
    DateTimeOffset ScheduledAt,
    string Status,
    int SetCount);

public record CreateWorkoutRequest(
    string Name,
    DateTimeOffset ScheduledAt,
    string? Notes);

public record UpdateWorkoutRequest(
    string? Name,
    DateTimeOffset? ScheduledAt,
    string? Notes,
    WorkoutStatus? Status,
    int? SessionRpe);
