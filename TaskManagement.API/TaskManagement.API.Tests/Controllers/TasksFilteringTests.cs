using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.API.Tests.Infrastructure;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.API.Tests.Controllers;

public sealed class TasksFilteringTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TasksFilteringTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetPaged_WithStatusFilter_ShouldReturnMatchingTasks()
    {
        var token = await RegisterAndGetTokenAsync();

        await CreateTaskAsync(token, "Pending task");
        var completedId = await CreateTaskAsync(token, "Completed task");

        await UpdateTaskStatusAsync(
                token,
                completedId,
                "Completed task",
                TaskStatus.Completed);

        SetAuthorization(token);

        var response = await _client.GetAsync(
            $"/api/tasks/paged?status={(int)TaskStatus.Completed}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Single(result.Items);
        Assert.Equal(completedId, result.Items[0].Id);
        Assert.Equal(TaskStatus.Completed, result.Items[0].Status);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_WithTitleFilter_ShouldReturnMatchingTasks()
    {
        var token = await RegisterAndGetTokenAsync();

        await CreateTaskAsync(token, "Implement JWT");
        await CreateTaskAsync(token, "Configure database");
        await CreateTaskAsync(token, "Review JWT security");

        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?title=JWT");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items,
            task => Assert.Contains("JWT", task.Title));
    }

    [Fact]
    public async Task GetPaged_SortByTitleAscending_ShouldReturnCorrectOrder()
    {
        var token = await RegisterAndGetTokenAsync();

        await CreateTaskAsync(token, "Zulu");
        await CreateTaskAsync(token, "Alpha");
        await CreateTaskAsync(token, "Bravo");

        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?sortBy=title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Equal(
            new[] { "Alpha", "Bravo", "Zulu" },
            result.Items.Select(x => x.Title));
    }

    [Fact]
    public async Task GetPaged_SortByTitleDescending_ShouldReturnCorrectOrder()
    {
        var token = await RegisterAndGetTokenAsync();

        await CreateTaskAsync(token, "Zulu");
        await CreateTaskAsync(token, "Alpha");
        await CreateTaskAsync(token, "Bravo");

        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?sortBy=title&sortDirection=desc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Equal(
            new[] { "Zulu", "Bravo", "Alpha" },
            result.Items.Select(x => x.Title));
    }


    [Fact]
    public async Task GetPaged_WithCombinedFilters_ShouldReturnCorrectTotal()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();

        // Task 1: matches both filters
        var matchingId = await CreateTaskAsync(
            token,
            "JWT authentication");

        await UpdateTaskStatusAsync(
            token,
            matchingId,
            "JWT authentication",
            TaskStatus.Completed);

        // Task 2: matches title but not status
        await CreateTaskAsync(
            token,
            "JWT pending");

        // Task 3: matches status but not title
        var otherId = await CreateTaskAsync(
            token,
            "Database completed");

        await UpdateTaskStatusAsync(
            token,
            otherId,
            "Database completed",
            TaskStatus.Completed);

        // Act
        SetAuthorization(token);

        var response = await _client.GetAsync(
            $"/api/tasks/paged?status={(int)TaskStatus.Completed}&title=JWT");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Single(result.Items);
        Assert.Equal(matchingId, result.Items[0].Id);
        Assert.Equal("JWT authentication", result.Items[0].Title);
        Assert.Equal(TaskStatus.Completed, result.Items[0].Status);
        Assert.Equal(1, result.TotalCount);
    }


    [Theory]
    [InlineData("invalidField", "asc", "SortBy")]
    [InlineData("title", "invalid", "SortDirection")]
    public async Task GetPaged_WithInvalidSorting_ShouldReturnBadRequest(
        string sortBy,
        string sortDirection,
        string invalidProperty)
    {
        var token = await RegisterAndGetTokenAsync();
        SetAuthorization(token);

        var response = await _client.GetAsync(
            $"/api/tasks/paged?sortBy={sortBy}&sortDirection={sortDirection}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(invalidProperty, problem.Errors.Keys);
    }

    [Fact]
    public async Task GetPaged_WithInvalidStatus_ShouldReturnBadRequest()
    {
        var token = await RegisterAndGetTokenAsync();
        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?status=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("Status", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetPaged_ShouldNotExposeOtherUsersTasks()
    {
        var tokenA = await RegisterAndGetTokenAsync();
        await CreateTaskAsync(tokenA, "User A secret");

        var tokenB = await RegisterAndGetTokenAsync();
        await CreateTaskAsync(tokenB, "User B task");

        SetAuthorization(tokenA);

        var response = await _client.GetAsync(
            "/api/tasks/paged?title=User B");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedAsync(response);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var request = new RegisterRequestDto
        {
            UserName = "filter-user",
            Email = $"filter-{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword@2026"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);
        return auth.AccessToken;
    }

    private async Task<Guid> CreateTaskAsync(
        string token,
        string title)
    {
        SetAuthorization(token);

        var request = new CreateTaskDto(
            title,
            "Filtering integration test",
            DateTime.UtcNow.AddDays(7));

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content
            .ReadFromJsonAsync<TaskResponseDto>();

        Assert.NotNull(task);
        return task.Id;
    }

    private async Task UpdateTaskStatusAsync(
        string token,
        Guid taskId,
        string title,
        TaskStatus status)
    {
        SetAuthorization(token);

        var request = new UpdateTaskDto(
            title,
            "Updated description",
            DateTime.UtcNow.AddDays(7),
            status);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private void SetAuthorization(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<PagedResult<TaskResponseDto>>
        ReadPagedAsync(HttpResponseMessage response)
    {
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<TaskResponseDto>>();

        Assert.NotNull(result);
        return result;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}