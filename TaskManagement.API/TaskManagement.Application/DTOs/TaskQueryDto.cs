using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

public sealed class TaskQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public TaskStatus? Status { get; set; }
    public string? Title { get; set; }

    public string SortBy { get; set; } = "dueDate";
    public string SortDirection { get; set; } = "asc";
}
