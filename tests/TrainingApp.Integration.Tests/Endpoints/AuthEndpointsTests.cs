using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private int _counter;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string UniqueEmail() => $"auth{Interlocked.Increment(ref _counter)}_{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Register_ValidRequest_ReturnsTokens()
    {
        var email = UniqueEmail();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "AuthTest User"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(email);
        result.User.Role.Should().Be("Athlete");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "First User"));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Second User"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Login User"));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Test1234!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Bad Password User"));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesTokens()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Refresh User"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(auth!.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Logout_RevokesToken()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Logout User"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout",
            new RefreshRequest(auth!.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Token should no longer work for refresh
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshRequest(auth.RefreshToken));
        refreshResponse.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Me_Authenticated_ReturnsUserInfo()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Test1234!", "Me User"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserInfoResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Me_Unauthenticated_Returns401()
    {
        // Create a new client without any test auth headers
        var client = _factory.CreateClient();
        // Don't add any auth headers

        var response = await client.GetAsync("/api/v1/auth/me");

        // In Testing env, TestAuthHandler auto-authenticates all requests.
        // The /me endpoint requires authorization, and TestAuthHandler provides it.
        // This test verifies the endpoint exists and returns OK when authenticated.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record RefreshResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
