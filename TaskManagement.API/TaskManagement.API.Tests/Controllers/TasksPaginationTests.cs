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

namespace TaskManagement.API.Tests.Controllers;

public sealed class TasksPaginationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TasksPaginationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetPaged_FirstPage_ShouldReturnCorrectItems()
    {
        var token = await RegisterAndGetTokenAsync();
        await CreateTasksAsync(token, 5);

        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedResultAsync(response);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ShouldNotRepeatItems()
    {
        var token = await RegisterAndGetTokenAsync();
        await CreateTasksAsync(token, 5);

        SetAuthorization(token);

        var firstResponse = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=1&pageSize=2");

        var secondResponse = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var first = await ReadPagedResultAsync(firstResponse);
        var second = await ReadPagedResultAsync(secondResponse);

        Assert.Equal(2, first.Items.Count);
        Assert.Equal(2, second.Items.Count);

        Assert.Empty(
            first.Items.Select(x => x.Id)
                .Intersect(second.Items.Select(x => x.Id)));
    }

    [Fact]
    public async Task GetPaged_BeyondLastPage_ShouldReturnEmptyItems()
    {
        var token = await RegisterAndGetTokenAsync();
        await CreateTasksAsync(token, 3);

        SetAuthorization(token);

        var response = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=10&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedResultAsync(response);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPaged_WithInvalidParameters_ShouldReturnBadRequest(
        int pageNumber,
        int pageSize)
    {
        var token = await RegisterAndGetTokenAsync();
        SetAuthorization(token);

        var response = await _client.GetAsync(
            $"/api/tasks/paged?pageNumber={pageNumber}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);

        if (pageNumber < 1)
            Assert.Contains("PageNumber", problem.Errors.Keys);
        else
            Assert.Contains("PageSize", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetPaged_ShouldReturnOnlyAuthenticatedUserTasks()
    {
        var tokenA = await RegisterAndGetTokenAsync();
        await CreateTasksAsync(tokenA, 3);

        var tokenB = await RegisterAndGetTokenAsync();
        await CreateTasksAsync(tokenB, 2);

        SetAuthorization(tokenA);

        var response = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await ReadPagedResultAsync(response);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetPaged_WithoutJwt_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/tasks/paged?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var request = new RegisterRequestDto
        {
            UserName = "pagination-user",
            Email = $"pagination-{Guid.NewGuid():N}@example.com",
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

    private async Task CreateTasksAsync(string token, int count)
    {
        SetAuthorization(token);

        for (var i = 0; i < count; i++)
        {
            var request = new CreateTaskDto(
                $"Pagination task {i}",
                $"Description {i}",
                DateTime.UtcNow.AddDays(i + 1));

            var response = await _client.PostAsJsonAsync(
                "/api/tasks", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    private void SetAuthorization(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<PagedResult<TaskResponseDto>>
        ReadPagedResultAsync(HttpResponseMessage response)
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