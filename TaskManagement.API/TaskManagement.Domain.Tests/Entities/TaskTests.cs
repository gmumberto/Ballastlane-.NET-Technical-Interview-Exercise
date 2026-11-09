using Xunit;
using DomainTask = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Tests.Entities;

public class TaskTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        var task = new DomainTask(
            "Implement authentication",
            "Configure JWT authentication",
            dueDate,
            userId);

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Implement authentication", task.Title);
        Assert.Equal(userId, task.UserId);
        Assert.Equal(TaskStatus.Pending, task.Status);
        Assert.Equal(dueDate, task.DueDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTitle_ShouldThrow(
        string? title)
    {
        Assert.Throws<ArgumentException>(() =>
            new DomainTask(
                title!,
                "Description",
                DateTime.UtcNow.AddDays(1),
                Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new DomainTask(
                "Valid title",
                "Description",
                DateTime.UtcNow.AddDays(1),
                Guid.Empty));
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateTask()
    {
        // Arrange
        var task = new DomainTask(
            "Original title",
            "Original description",
            DateTime.UtcNow.AddDays(3),
            Guid.NewGuid());

        var newDueDate = DateTime.UtcNow.AddDays(10);

        // Act
        task.Update(
            "Updated title",
            "Updated description",
            newDueDate);

        // Assert
        Assert.Equal("Updated title", task.Title);
        Assert.Equal("Updated description", task.Description);
        Assert.Equal(newDueDate, task.DueDate);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WithValidStatus_ShouldUpdate()
    {
        var task = new DomainTask(
            "Test task",
            "Description",
            DateTime.UtcNow.AddDays(1),
            Guid.NewGuid());

        task.ChangeStatus(TaskStatus.Completed);

        Assert.Equal(TaskStatus.Completed, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WithInvalidStatus_ShouldThrow()
    {
        var task = new DomainTask(
            "Test task",
            "Description",
            DateTime.UtcNow.AddDays(1),
            Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            task.ChangeStatus((TaskStatus)999));
    }
}