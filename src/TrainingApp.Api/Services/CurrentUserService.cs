using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _env;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    public Guid UserId
    {
        get
        {
            // Multi-user test support: allow overriding user via header in Testing env
            var testHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Test-UserId"].FirstOrDefault();
            if (_env.IsEnvironment("Testing") && Guid.TryParse(testHeader, out var testUserId))
                return testUserId;

            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            // Fallback for development/testing until auth is implemented
            return DefaultUserId;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
