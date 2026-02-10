using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly TrainingAppDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly JwtSettings _settings;

    public JwtTokenService(
        TrainingAppDbContext db,
        UserManager<User> userManager,
        IOptions<JwtSettings> settings)
    {
        _db = db;
        _userManager = userManager;
        _settings = settings.Value;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)> GenerateTokensAsync(
        User user, CancellationToken ct = default)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        // Resolve subscription tier
        var subscription = await _db.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id, ct);

        var effectiveTier = ResolveEffectiveTier(subscription);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("display_name", user.DisplayName),
            new("subscription_tier", effectiveTier.ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var accessToken = GenerateJwt(claims, expiresAt);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = HashToken(refreshToken),
            Jti = jti,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpiryDays)
        };

        _db.Set<RefreshToken>().Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        return (accessToken, refreshToken, expiresAt);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)> RefreshTokensAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);

        var stored = await _db.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash, ct)
            ?? throw new ValidationException("RefreshToken", "Invalid refresh token");

        if (stored.IsRevoked)
        {
            // Possible token reuse — revoke entire chain
            await RevokeDescendantsAsync(stored, ct);
            throw new ValidationException("RefreshToken", "Token has been revoked");
        }

        if (stored.IsExpired)
            throw new ValidationException("RefreshToken", "Token has expired");

        // Rotate: revoke old, issue new
        stored.RevokedAt = DateTimeOffset.UtcNow;
        var (accessToken, newRefreshToken, expiresAt) = await GenerateTokensAsync(stored.User!, ct);
        stored.ReplacedByToken = HashToken(newRefreshToken);

        await _db.SaveChangesAsync(ct);

        return (accessToken, newRefreshToken, expiresAt);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);

        var stored = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash, ct);

        if (stored is null || stored.IsRevoked) return;

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private string GenerateJwt(List<Claim> claims, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task RevokeDescendantsAsync(RefreshToken token, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token.ReplacedByToken)) return;

        var child = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == token.ReplacedByToken, ct);

        if (child is not null && !child.IsRevoked)
        {
            child.RevokedAt = DateTimeOffset.UtcNow;
            await RevokeDescendantsAsync(child, ct);
        }
    }

    private static SubscriptionTier ResolveEffectiveTier(UserSubscription? subscription)
    {
        if (subscription is null)
            return SubscriptionTier.Athlete;

        if (subscription.Status == SubscriptionStatus.Trial
            && subscription.TrialEndDate.HasValue
            && subscription.TrialEndDate.Value < DateTimeOffset.UtcNow)
            return SubscriptionTier.Athlete;

        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            return SubscriptionTier.Athlete;

        return subscription.Tier;
    }
}
