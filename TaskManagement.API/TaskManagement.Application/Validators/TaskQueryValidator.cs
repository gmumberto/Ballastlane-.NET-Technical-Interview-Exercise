using FluentValidation;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Validators;

public sealed class TaskQueryValidator
    : AbstractValidator<TaskQueryDto>
{
    public TaskQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(status.Value))
            .WithMessage("Invalid task status.");

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => x.Title is not null)
            .WithMessage("Title filter must not exceed 200 characters.");

        RuleFor(x => x.SortBy)
            .NotEmpty()
            .Must(value =>
                new[] { "dueDate", "createdAt", "title" }
                    .Contains(
                        value,
                        StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be dueDate, createdAt or title.");

        RuleFor(x => x.SortDirection)
            .NotEmpty()
            .Must(value =>
                string.Equals(value, "asc",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "desc",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortDirection must be asc or desc.");
    }
}