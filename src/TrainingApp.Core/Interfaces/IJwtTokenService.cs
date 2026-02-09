using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IJwtTokenService
{
    Task<(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)> GenerateTokensAsync(User user, CancellationToken ct = default);
    Task<(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)> RefreshTokensAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
}
