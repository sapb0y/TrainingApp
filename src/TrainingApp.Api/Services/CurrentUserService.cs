using System.Security.Claims;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            throw new InvalidOperationException("User ID claim not found. Ensure the request is authenticated.");
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsCoach =>
        _httpContextAccessor.HttpContext?.User.IsInRole("Coach") ?? false;

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
