using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.API.Tests.Infrastructure;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure.Persistence;
using Xunit;

namespace TaskManagement.API.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated()
    {
        var request = CreateRegisterRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldPersistUser()
    {
        var request = CreateRegisterRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == request.Email);

        Assert.NotNull(user);
        Assert.Equal(request.UserName, user.UserName);
        Assert.NotEqual(request.Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        var request = CreateRegisterRequest();

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturnBadRequest()
    {
        var request = CreateRegisterRequest();
        request.Password = "123456";

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var register = CreateRegisterRequest();

        var registration = await _client.PostAsJsonAsync(
            "/api/auth/register", register);

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = new LoginRequestDto
        {
            Email = register.Email,
            Password = register.Password
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", login);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        var register = CreateRegisterRequest();

        await _client.PostAsJsonAsync(
            "/api/auth/register", register);

        var login = new LoginRequestDto
        {
            Email = register.Email,
            Password = "IncorrectPassword@2026"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", login);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ShouldReturnUnauthorized()
    {
        var login = new LoginRequestDto
        {
            Email = "unknown@example.com",
            Password = "StrongPassword@2026"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", login);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Tasks_WithoutJwt_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Tasks_WithValidJwt_ShouldReturnOk()
    {
        var register = CreateRegisterRequest();

        var registration = await _client.PostAsJsonAsync(
            "/api/auth/register", register);

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var auth = await registration.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithConcurrentDuplicateEmail_ShouldCreateOnlyOneUser()
    {
        // Arrange
        var request = CreateRegisterRequest();

        // Act
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/auth/register", request),
            _client.PostAsJsonAsync("/api/auth/register", request));

        // Assert
        Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Created);

        Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var count = await context.Users.CountAsync(
            user => user.Email == request.Email);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnProblemDetails()
    {
        // Arrange
        var request = CreateRegisterRequest();

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal(
            "Email is already registered.",
            problem.Detail);
    }

    private static RegisterRequestDto CreateRegisterRequest()
    {
        return new RegisterRequestDto
        {
            UserName = "IntegrationTestUser",
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword@2026"
        };
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}