using TaskState = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Entities;

public class Task
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TaskState Status { get; private set; }
    public DateTime DueDate { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Task()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    public Task(
        string title,
        string description,
        DateTime dueDate,
        Guid userId)
    {
        ValidateTitle(title);

        if (userId == Guid.Empty)
            throw new ArgumentException(
                "UserId cannot be empty.");

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description ?? string.Empty;
        DueDate = dueDate;
        UserId = userId;
        Status = TaskState.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string description,
        DateTime dueDate)
    {
        ValidateTitle(title);

        Title = title.Trim();
        Description = description ?? string.Empty;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TaskState status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException(
                "Invalid task status.");

        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Task title is required.");

        if (title.Length > 200)
            throw new ArgumentException(
                "Task title cannot exceed 200 characters.");
    }
}
