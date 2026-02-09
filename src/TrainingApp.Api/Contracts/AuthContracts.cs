namespace TrainingApp.Api.Contracts;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserInfoResponse User);
public record UserInfoResponse(Guid Id, string Email, string DisplayName, string? Role, string Tier);
