using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<Result<IEnumerable<TaskResponseDto>>> GetAllAsync(
    Guid userId,
    CancellationToken cancellationToken = default);

    Task<Result<TaskResponseDto>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<TaskResponseDto>> CreateAsync(
        CreateTaskDto request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateAsync(
        Guid id,
        UpdateTaskDto request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<TaskResponseDto>>> GetPagedAsync(
        Guid userId,
        TaskQueryDto query,
        CancellationToken cancellationToken = default);
}