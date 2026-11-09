using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using Xunit;
using DomainTask = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repositoryMock;
    private readonly TaskService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _repositoryMock = new Mock<ITaskRepository>();
        _service = new TaskService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateTask()
    {
        var request = new CreateTaskDto(
            "Implement authentication",
            "Configure JWT",
            DateTime.UtcNow.AddDays(7));

        var result = await _service.CreateAsync(request, _userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Title, result.Value.Title);
        Assert.Equal(_userId, result.Value.UserId);
        Assert.Equal(TaskStatus.Pending, result.Value.Status);

        _repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<DomainTask>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ShouldReturnTask()
    {
        var task = CreateTask();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(
                task.Id,
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _service.GetByIdAsync(
            task.Id, _userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(task.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync(
            Guid.NewGuid(), _userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Task.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserTasks()
    {
        var tasks = new List<DomainTask>
        {
            CreateTask(),
            CreateTask()
        };

        _repositoryMock
            .Setup(r => r.GetAllByUserIdAsync(
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var result = await _service.GetAllAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskExists_ShouldUpdate()
    {
        var task = CreateTask();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(
                task.Id,
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var request = new UpdateTaskDto(
            "Updated task",
            "Updated description",
            DateTime.UtcNow.AddDays(10),
            TaskStatus.Completed);

        var result = await _service.UpdateAsync(
            task.Id, request, _userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated task", task.Title);
        Assert.Equal(TaskStatus.Completed, task.Status);

        _repositoryMock.Verify(
            r => r.UpdateAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskDoesNotExist_ShouldReturnFalse()
    {
        var request = new UpdateTaskDto(
            "Updated task",
            "Description",
            DateTime.UtcNow.AddDays(5),
            TaskStatus.Pending);

        var result = await _service.UpdateAsync(
            Guid.NewGuid(), request, _userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Task.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_ShouldDelete()
    {
        var task = CreateTask();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(
                task.Id,
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _service.DeleteAsync(
            task.Id, _userId);

        Assert.True(result.IsSuccess);

        _repositoryMock.Verify(
            r => r.DeleteAsync(
                task,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotExist_ShouldReturnFalse()
    {
        var result = await _service.DeleteAsync(
            Guid.NewGuid(), _userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Task.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTitle_ShouldReturnValidationFailure()
    {
        var request = new CreateTaskDto(
            "",
            "Description",
            DateTime.UtcNow.AddDays(1));

        var result = await _service.CreateAsync(request, _userId);
        Assert.False(result.IsSuccess);
        Assert.Equal("Task.Validation", result.Error?.Code);

        _repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<DomainTask>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidStatus_ShouldReturnValidationFailure()
    {
        var request = new UpdateTaskDto(
            "Valid title", "Description", DateTime.UtcNow.AddDays(1), (TaskStatus)0);
        var result = await _service.UpdateAsync(Guid.NewGuid(), request, _userId);
        Assert.False(result.IsSuccess);
        Assert.Equal("Task.Validation", result.Error?.Code);
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.IsAny<DomainTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidPage_ShouldReturnValidationFailure()
    {
        var result = await _service.GetPagedAsync(_userId, new TaskQueryDto
        {
            PageNumber = 0, PageSize = 10
        });
        Assert.False(result.IsSuccess);
        Assert.Equal("Task.Validation", result.Error?.Code);
        _repositoryMock.Verify(r => r.GetPagedByUserIdAsync(
            It.IsAny<Guid>(), It.IsAny<TaskQueryDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private DomainTask CreateTask()
    {
        return new DomainTask(
            "Test task",
            "Test description",
            DateTime.UtcNow.AddDays(7),
            _userId);
    }
}