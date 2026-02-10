namespace TrainingApp.Api.Contracts;

public record SubmitCoachApplicationRequest(
    string Credentials,
    int CurrentClientCount,
    string BusinessGoal,
    string? AdditionalInfo);

public record ReviewCoachApplicationRequest(bool Approve, string? Notes);

public record CoachApplicationResponse(
    Guid Id,
    Guid UserId,
    string? UserDisplayName,
    string Credentials,
    int CurrentClientCount,
    string BusinessGoal,
    string? AdditionalInfo,
    string Status,
    string? ReviewNotes,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt);
