using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Validators;
using DomainTask = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TaskResponseDto>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.GetAllByUserIdAsync(
            userId, cancellationToken);

        return Result<IEnumerable<TaskResponseDto>>.Success(tasks.Select(MapToDto).ToList());
    }

    public async Task<Result<TaskResponseDto>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(
            id, userId, cancellationToken);

        return task is null
            ? Result<TaskResponseDto>.Failure(TaskNotFound())
            : Result<TaskResponseDto>.Success(MapToDto(task));
    }

    public async Task<Result<TaskResponseDto>> CreateAsync(
        CreateTaskDto request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var validation = new CreateTaskValidator().Validate(request);
        if (!validation.IsValid)
            return Result<TaskResponseDto>.Failure(InvalidTask(validation.Errors[0].ErrorMessage));
        if (userId == Guid.Empty)
            return Result<TaskResponseDto>.Failure(InvalidTask("Invalid user identifier."));

        var task = new DomainTask(
            request.Title,
            request.Description,
            request.DueDate,
            userId);

        await _repository.AddAsync(
            task, cancellationToken);

        return Result<TaskResponseDto>.Success(MapToDto(task));
    }

    public async Task<Result<bool>> UpdateAsync(
        Guid id,
        UpdateTaskDto request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var validation = new UpdateTaskValidator().Validate(request);
        if (!validation.IsValid)
            return Result<bool>.Failure(InvalidTask(validation.Errors[0].ErrorMessage));

        var task = await _repository.GetByIdAsync(
            id, userId, cancellationToken);

        if (task is null)
            return Result<bool>.Failure(TaskNotFound());

        task.Update(
            request.Title,
            request.Description,
            request.DueDate);

        task.ChangeStatus(request.Status);

        await _repository.UpdateAsync(
            task, cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(
            id, userId, cancellationToken);

        if (task is null)
            return Result<bool>.Failure(TaskNotFound());

        await _repository.DeleteAsync(
            task, cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<TaskResponseDto>>> GetPagedAsync(
        Guid userId,
        TaskQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var validation = new TaskQueryValidator().Validate(query);
        if (!validation.IsValid)
            return Result<PagedResult<TaskResponseDto>>.Failure(
                InvalidTask(validation.Errors[0].ErrorMessage));

        var result = await _repository.GetPagedByUserIdAsync(
            userId,
            query,
            cancellationToken);

        var items = result.Items
            .Select(MapToDto)
            .ToList();

        return Result<PagedResult<TaskResponseDto>>.Success(
            new PagedResult<TaskResponseDto>(
                items, result.PageNumber, result.PageSize, result.TotalCount));
    }

    private static AppError InvalidTask(string message) =>
        new(ErrorType.Validation, "Task.Validation", message);

    private static AppError TaskNotFound() =>
        new(ErrorType.NotFound, "Task.NotFound", "Task not found.");

    private static TaskResponseDto MapToDto(DomainTask task)
    {
        return new TaskResponseDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.UserId,
            task.CreatedAt,
            task.UpdatedAt);
    }
}