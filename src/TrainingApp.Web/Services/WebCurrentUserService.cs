using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Web.Services;

public class WebCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbContextFactory<TrainingAppDbContext> _dbFactory;
    private SubscriptionTier? _cachedTier;

    public WebCurrentUserService(IHttpContextAccessor httpContextAccessor, IDbContextFactory<TrainingAppDbContext> dbFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbFactory = dbFactory;
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
            if (_cachedTier.HasValue) return _cachedTier.Value;

            if (!IsAuthenticated) return SubscriptionTier.Athlete;

            using var db = _dbFactory.CreateDbContext();
            var sub = db.UserSubscriptions.AsNoTracking()
                .Where(s => s.UserId == UserId)
                .Select(s => s.Tier)
                .FirstOrDefault();

            _cachedTier = sub == default ? SubscriptionTier.Athlete : sub;
            return _cachedTier.Value;
        }
    }
}
