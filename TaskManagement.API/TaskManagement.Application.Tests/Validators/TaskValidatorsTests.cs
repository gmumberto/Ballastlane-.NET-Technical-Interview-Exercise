using TaskManagement.Application.DTOs;
using TaskManagement.Application.Validators;
using TaskState = TaskManagement.Domain.Enums.TaskStatus;
using Xunit;

namespace TaskManagement.Application.Tests.Validators;

public class TaskValidatorsTests
{
    private readonly CreateTaskValidator _createValidator = new();
    private readonly UpdateTaskValidator _updateValidator = new();

    [Fact]
    public async Task Create_WithValidRequest_ShouldPass()
    {
        var request = new CreateTaskDto(
            "Implement authentication",
            "Add JWT authentication.",
            DateTime.UtcNow.AddDays(7));

        var result = await _createValidator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ShouldFail()
    {
        var request = new CreateTaskDto(
            "",
            "Task description",
            DateTime.UtcNow.AddDays(7));

        var result = await _createValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public async Task Create_WithLongTitle_ShouldFail()
    {
        var request = new CreateTaskDto(
            new string('A', 201),
            "Task description",
            DateTime.UtcNow.AddDays(7));

        var result = await _createValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public async Task Create_WithInvalidDueDate_ShouldFail()
    {
        var request = new CreateTaskDto(
            "Task title",
            "Task description",
            default);

        var result = await _createValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "DueDate");
    }


    [Fact]
    public async Task Update_WithValidRequest_ShouldPass()
    {
        // Arrange
        var validStatus = Enum.GetValues<TaskState>().First();

        var request = new UpdateTaskDto(
            "Updated task",
            "Updated description",
            DateTime.UtcNow.AddDays(5),
            validStatus);

        // Act
        var result = await _updateValidator.ValidateAsync(request);

        // Assert
        Assert.True(
            result.IsValid,
            string.Join("; ", result.Errors.Select(
                error => $"{error.PropertyName}: {error.ErrorMessage}")));
    }

    [Fact]
    public async Task Update_WithInvalidStatus_ShouldFail()
    {
        var request = new UpdateTaskDto(
            "Updated task",
            "Updated description",
            DateTime.UtcNow.AddDays(5),
            (TaskState)999);

        var result = await _updateValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Status");
    }

    [Fact]
    public async Task Update_WithEmptyTitle_ShouldFail()
    {
        var request = new UpdateTaskDto(
            "",
            "Updated description",
            DateTime.UtcNow.AddDays(5),
            (TaskState)0);

        var result = await _updateValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public async Task Update_WithInvalidDueDate_ShouldFail()
    {
        var request = new UpdateTaskDto(
            "Updated task",
            "Updated description",
            default,
            (TaskState)0);

        var result = await _updateValidator.ValidateAsync(request);

        Assert.Contains(result.Errors,
            error => error.PropertyName == "DueDate");
    }
}