namespace TaskManagement.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string Description,
    DateTime DueDate);
