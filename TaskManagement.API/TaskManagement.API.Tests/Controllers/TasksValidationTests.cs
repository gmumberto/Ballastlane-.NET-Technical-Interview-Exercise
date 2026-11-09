using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.API.Tests.Infrastructure;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.API.Tests.Controllers;

public sealed class TasksValidationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TasksValidationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ShouldReturnBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();
        SetAuthorization(token);

        var request = new CreateTaskDto(
            "",
            "Valid description",
            DateTime.UtcNow.AddDays(7));

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("Title", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithInvalidDueDate_ShouldReturnBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();
        SetAuthorization(token);

        var request = new CreateTaskDto(
            "Valid title",
            "Valid description",
            default);

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("DueDate", problem.Errors.Keys);
    }

    [Fact]
    public async Task Update_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();
        var taskId = await CreateTaskAsync(token);

        SetAuthorization(token);

        var request = new UpdateTaskDto(
            "Invalid update",
            "Attempt to update with invalid status",
            DateTime.UtcNow.AddDays(10),
            (TaskStatus)999);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("Status", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithoutJwt_ShouldReturnUnauthorized()
    {
        var request = new CreateTaskDto(
            "",
            "Invalid request without authentication",
            default);

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutJwt_ShouldReturnUnauthorized()
    {
        var request = new UpdateTaskDto(
            "",
            "Invalid update without authentication",
            default,
            (TaskStatus)999);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{Guid.NewGuid()}",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithInvalidStatus_ShouldNotModifyTask()
    {
        var token = await RegisterAndGetTokenAsync();
        var taskId = await CreateTaskAsync(token);

        SetAuthorization(token);

        var invalidRequest = new UpdateTaskDto(
            "Modified incorrectly",
            "This update must be rejected",
            DateTime.UtcNow.AddDays(15),
            (TaskStatus)999);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            invalidRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            updateResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var task = await getResponse.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);
        Assert.Equal("Validation test task", task.Title);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var request = new RegisterRequestDto
        {
            UserName = "validation-user",
            Email = $"validation-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword@2026"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);
        Assert.False(
            string.IsNullOrWhiteSpace(auth.AccessToken));

        return auth.AccessToken;
    }

    private async Task<Guid> CreateTaskAsync(string token)
    {
        SetAuthorization(token);

        var request = new CreateTaskDto(
            "Validation test task",
            "Original task description",
            DateTime.UtcNow.AddDays(7));

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var task = await response.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);

        return task.Id;
    }

    private void SetAuthorization(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}