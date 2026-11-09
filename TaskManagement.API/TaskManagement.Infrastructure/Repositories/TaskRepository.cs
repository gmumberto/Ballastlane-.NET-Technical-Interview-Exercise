using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Persistence;
using DomainTask = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DomainTask>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<DomainTask?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        DomainTask task,
        CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(
            task, cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        DomainTask task,
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        DomainTask task,
        CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PagedResult<DomainTask>> GetPagedByUserIdAsync(
        Guid userId,
        TaskQueryDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.PageNumber < 1)
            throw new ArgumentOutOfRangeException(
                nameof(request.PageNumber));

        if (request.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(request.PageSize));

        if (request.Status.HasValue &&
            !Enum.IsDefined(request.Status.Value))
            throw new ArgumentException("Invalid task status.");

        var sortBy = request.SortBy?.ToLowerInvariant()
            ?? throw new ArgumentException("SortBy is required.");

        var sortDirection = request.SortDirection?.ToLowerInvariant()
            ?? throw new ArgumentException("SortDirection is required.");

        if (sortBy is not ("duedate" or "createdat" or "title"))
            throw new ArgumentException("Invalid sort field.");

        if (sortDirection is not ("asc" or "desc"))
            throw new ArgumentException("Invalid sort direction.");

        var query = _context.Tasks
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        // Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(
                x => x.Status == request.Status.Value);
        }

        // Title filter
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var title = request.Title.Trim();

            query = query.Where(
                x => x.Title.Contains(title));
        }

        // Count after filtering, before pagination
        var totalCount = await query.CountAsync(
            cancellationToken);

        // Dynamic sorting with deterministic tie-breaker
        var descending = sortDirection == "desc";

        query = (sortBy, descending) switch
        {
            ("title", false) => query
                .OrderBy(x => x.Title)
                .ThenBy(x => x.Id),

            ("title", true) => query
                .OrderByDescending(x => x.Title)
                .ThenBy(x => x.Id),

            ("createdat", false) => query
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id),

            ("createdat", true) => query
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id),

            ("duedate", true) => query
                .OrderByDescending(x => x.DueDate)
                .ThenBy(x => x.Id),

            _ => query
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.Id)
        };

        var skip = ((long)request.PageNumber - 1)
            * request.PageSize;

        if (skip > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(request.PageNumber));

        var tasks = await query
            .Skip((int)skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DomainTask>(
            tasks,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}