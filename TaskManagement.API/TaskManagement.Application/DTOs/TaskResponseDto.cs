using TaskState = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

public record TaskResponseDto(
    Guid Id,
    string Title,
    string Description,
    TaskState Status,
    DateTime DueDate,
    Guid UserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
