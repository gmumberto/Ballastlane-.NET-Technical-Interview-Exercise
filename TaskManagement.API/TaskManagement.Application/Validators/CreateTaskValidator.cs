using FluentValidation;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Validators;

public sealed class CreateTaskValidator
    : AbstractValidator<CreateTaskDto>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Task title is required.")
            .MaximumLength(200)
            .WithMessage("Task title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("Task description cannot be null.")
            .MaximumLength(2000)
            .WithMessage("Task description must not exceed 2000 characters.");

        RuleFor(x => x.DueDate)
            .Must(date => date != default)
            .WithMessage("Task due date is required.");
    }
}