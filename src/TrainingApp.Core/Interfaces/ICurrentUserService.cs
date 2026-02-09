namespace TrainingApp.Core.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
    bool IsCoach { get; }
}
