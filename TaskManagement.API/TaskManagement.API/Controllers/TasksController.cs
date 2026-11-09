using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Manages tasks belonging to the authenticated user.
/// </summary>
/// <remarks>
/// All endpoints require JWT Bearer authentication.
///
/// Users can only access, modify or delete their own tasks.
/// </remarks>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
[Tags("Tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "Invalid authenticated user.");
        }

        return id;
    }

    /// <summary>
    /// Retrieves all tasks belonging to the authenticated user.
    /// </summary>
    /// <remarks>
    /// Returns all tasks without pagination.
    ///
    /// For large datasets, prefer GET /api/tasks/paged.
    /// </remarks>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>A collection of tasks.</returns>
    /// <response code="200">Tasks retrieved successfully.</response>
    /// <response code="401">Authentication is required.</response>
    [HttpGet]
    [ProducesResponseType(
        typeof(IEnumerable<TaskResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>>
        GetAll(CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAllAsync(
            GetUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ResultHttpMapper.ToProblem(result.Error!, HttpContext);
    }

    /// <summary>
    /// Retrieves tasks using pagination, filtering and sorting.
    /// </summary>
    /// <remarks>
    /// Supports the following query parameters:
    ///
    /// - pageNumber: Page number, starting at 1.
    /// - pageSize: Number of items per page, between 1 and 100.
    /// - status: Optional task status.
    /// - title: Optional partial title search.
    /// - sortBy: dueDate, createdAt or title.
    /// - sortDirection: asc or desc.
    ///
    /// Only tasks belonging to the authenticated user
    /// are included in the results.
    /// </remarks>
    /// <param name="query">
    /// Pagination, filtering and sorting options.
    /// </param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>
    /// A paginated result containing tasks and pagination metadata.
    /// </returns>
    /// <response code="200">Tasks retrieved successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="401">Authentication is required.</response>
    [HttpGet("paged")]
    [ProducesResponseType(
        typeof(PagedResult<TaskResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<TaskResponseDto>>> GetPaged(
        [FromQuery] TaskQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetPagedAsync(
            GetUserId(),
            query,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ResultHttpMapper.ToProblem(result.Error!, HttpContext);
    }

    /// <summary>
    /// Retrieves a task by its identifier.
    /// </summary>
    /// <param name="id">Unique task identifier.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>The requested task.</returns>
    /// <response code="200">Task found.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">Task not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(TaskResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(
            id,
            GetUserId(),
            cancellationToken);

        return task.IsSuccess
            ? Ok(task.Value)
            : ResultHttpMapper.ToProblem(task.Error!, HttpContext);
    }

    /// <summary>
    /// Creates a new task for the authenticated user.
    /// </summary>
    /// <param name="request">Task creation data.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <returns>The newly created task.</returns>
    /// <response code="201">Task created successfully.</response>
    /// <response code="400">Invalid task data.</response>
    /// <response code="401">Authentication is required.</response>
    [HttpPost]
    [ProducesResponseType(
        typeof(TaskResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaskResponseDto>> Create(
        [FromBody] CreateTaskDto request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(
            request,
            GetUserId(),
            cancellationToken);

        if (!task.IsSuccess)
            return ResultHttpMapper.ToProblem(task.Error!, HttpContext);

        return CreatedAtAction(
            nameof(GetById),
            new { id = task.Value!.Id },
            task.Value);
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <remarks>
    /// Only tasks belonging to the authenticated user
    /// can be updated.
    ///
    /// The request updates the title, description,
    /// due date and status.
    /// </remarks>
    /// <param name="id">Unique task identifier.</param>
    /// <param name="request">Updated task data.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <response code="204">Task updated successfully.</response>
    /// <response code="400">Invalid task data.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">Task not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTaskDto request,
        CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateAsync(
            id,
            request,
            GetUserId(),
            cancellationToken);

        if (!updated.IsSuccess)
            return ResultHttpMapper.ToProblem(updated.Error!, HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Deletes a task belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Unique task identifier.</param>
    /// <param name="cancellationToken">
    /// Request cancellation token.
    /// </param>
    /// <response code="204">Task deleted successfully.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="404">Task not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _taskService.DeleteAsync(
            id,
            GetUserId(),
            cancellationToken);

        if (!deleted.IsSuccess)
            return ResultHttpMapper.ToProblem(deleted.Error!, HttpContext);

        return NoContent();
    }
}