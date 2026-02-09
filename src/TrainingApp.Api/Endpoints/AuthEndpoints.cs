using Microsoft.AspNetCore.Identity;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithValidation<RegisterRequest>();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .WithValidation<LoginRequest>();

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithValidation<RefreshRequest>();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Revoke refresh token");

        group.MapGet("/me", Me)
            .WithName("GetCurrentUser")
            .WithSummary("Get current user info");
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<User> userManager,
        IJwtTokenService jwtService,
        CancellationToken ct)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description });
            throw new Core.Exceptions.ValidationException(errors);
        }

        await userManager.AddToRoleAsync(user, "Athlete");

        var (accessToken, refreshToken, expiresAt) = await jwtService.GenerateTokensAsync(user, ct);
        var userInfo = new UserInfoResponse(user.Id, user.Email!, user.DisplayName, "Athlete", "Competitor");

        return Results.Ok(new AuthResponse(accessToken, refreshToken, expiresAt, userInfo));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<User> userManager,
        IJwtTokenService jwtService,
        CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new Core.Exceptions.ValidationException("Credentials", "Invalid email or password");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        var (accessToken, refreshToken, expiresAt) = await jwtService.GenerateTokensAsync(user, ct);
        var userInfo = new UserInfoResponse(user.Id, user.Email!, user.DisplayName, role, "Competitor");

        return Results.Ok(new AuthResponse(accessToken, refreshToken, expiresAt, userInfo));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest request,
        IJwtTokenService jwtService,
        CancellationToken ct)
    {
        var (accessToken, refreshToken, expiresAt) = await jwtService.RefreshTokensAsync(request.RefreshToken, ct);
        return Results.Ok(new { accessToken, refreshToken, expiresAt });
    }

    private static async Task<IResult> Logout(
        RefreshRequest request,
        IJwtTokenService jwtService,
        CancellationToken ct)
    {
        await jwtService.RevokeTokenAsync(request.RefreshToken, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Me(
        ICurrentUserService currentUser,
        UserManager<User> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new Core.Exceptions.NotFoundException("User", currentUser.UserId.ToString());

        var roles = await userManager.GetRolesAsync(user);

        return Results.Ok(new UserInfoResponse(
            user.Id, user.Email!, user.DisplayName,
            roles.FirstOrDefault(), "Competitor"));
    }
}
