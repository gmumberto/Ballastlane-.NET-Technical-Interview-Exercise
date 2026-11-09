using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManagement.API.Tests.Infrastructure;
using TaskManagement.Application.DTOs;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.API.Tests.Controllers;

public class TasksControllerSecurityTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TasksControllerSecurityTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<
                TaskManagement.Infrastructure.Persistence.AppDbContext>();

        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetById_WhenTaskBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var tokenA = await RegisterAndGetTokenAsync("userA");
        var tokenB = await RegisterAndGetTokenAsync("userB");

        var taskId = await CreateTaskAsync(tokenA);

        // Act
        SetAuthorization(tokenB);

        var response = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyAuthenticatedUserTasks()
    {
        // Arrange
        var tokenA = await RegisterAndGetTokenAsync("userA");
        var tokenB = await RegisterAndGetTokenAsync("userB");

        var taskA = await CreateTaskAsync(tokenA);
        var taskB = await CreateTaskAsync(tokenB);

        // Act
        SetAuthorization(tokenA);

        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content
            .ReadFromJsonAsync<List<TaskResponseDto>>();

        Assert.NotNull(tasks);
        Assert.Single(tasks);
        Assert.Equal(taskA, tasks[0].Id);
        Assert.DoesNotContain(tasks, t => t.Id == taskB);
    }

    [Fact]
    public async Task Update_WhenTaskBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var tokenA = await RegisterAndGetTokenAsync("userA");
        var tokenB = await RegisterAndGetTokenAsync("userB");

        var taskId = await CreateTaskAsync(tokenA);

        var request = new UpdateTaskDto(
            "Unauthorized update",
            "Attempt to modify another user's task",
            DateTime.UtcNow.AddDays(10),
            TaskStatus.Completed);

        // Act
        SetAuthorization(tokenB);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify the original owner can still access the task.
        SetAuthorization(tokenA);

        var getResponse = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var task = await getResponse.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);
        Assert.NotEqual("Unauthorized update", task.Title);
    }

    [Fact]
    public async Task Delete_WhenTaskBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var tokenA = await RegisterAndGetTokenAsync("userA");
        var tokenB = await RegisterAndGetTokenAsync("userB");

        var taskId = await CreateTaskAsync(tokenA);

        // Act
        SetAuthorization(tokenB);

        var response = await _client.DeleteAsync(
            $"/api/tasks/{taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The task must still exist for its owner.
        SetAuthorization(tokenA);

        var getResponse = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenTaskBelongsToAuthenticatedUser_ShouldReturnOk()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("owner");

        var taskId = await CreateTaskAsync(token);

        // Act
        SetAuthorization(token);

        var response = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);
        Assert.Equal(taskId, task.Id);
    }

    [Fact]
    public async Task Update_WhenTaskBelongsToAuthenticatedUser_ShouldReturnNoContent()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("owner");

        var taskId = await CreateTaskAsync(token);

        var request = new UpdateTaskDto(
            "Updated by owner",
            "Valid update",
            DateTime.UtcNow.AddDays(14),
            TaskStatus.Completed);

        // Act
        SetAuthorization(token);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        var task = await getResponse.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);
        Assert.Equal("Updated by owner", task.Title);
        Assert.Equal(TaskStatus.Completed, task.Status);
    }

    [Fact]
    public async Task Delete_WhenTaskBelongsToAuthenticatedUser_ShouldReturnNoContent()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("owner");

        var taskId = await CreateTaskAsync(token);

        // Act
        SetAuthorization(token);

        var response = await _client.DeleteAsync(
            $"/api/tasks/{taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync(string username)
    {
        var request = new RegisterRequestDto
        {
            UserName = username,
            Email = $"{username}-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword@2026"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

        return auth.AccessToken;
    }

    private async Task<Guid> CreateTaskAsync(string token)
    {
        SetAuthorization(token);

        var request = new CreateTaskDto(
            "Security test task",
            "Task created for authorization testing",
            DateTime.UtcNow.AddDays(7));

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

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