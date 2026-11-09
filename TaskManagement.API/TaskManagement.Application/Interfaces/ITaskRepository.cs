using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using DomainTask = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<DomainTask>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DomainTask?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DomainTask task,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        DomainTask task,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        DomainTask task,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DomainTask>> GetPagedByUserIdAsync(
    Guid userId,
    TaskQueryDto query,
    CancellationToken cancellationToken = default);
}