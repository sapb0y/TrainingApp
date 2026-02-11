using System.Security.Claims;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Web.Services;

public class WebCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebCurrentUserService(IHttpContextAccessor httpContextAccessor)
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

            throw new InvalidOperationException("User ID claim not found.");
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsCoach =>
        _httpContextAccessor.HttpContext?.User.IsInRole("Coach") ?? false;

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;

    public SubscriptionTier Tier
    {
        get
        {
            var tierClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst("subscription_tier")?.Value;

            if (Enum.TryParse<SubscriptionTier>(tierClaim, true, out var tier))
                return tier;

            return SubscriptionTier.Athlete;
        }
    }
}
