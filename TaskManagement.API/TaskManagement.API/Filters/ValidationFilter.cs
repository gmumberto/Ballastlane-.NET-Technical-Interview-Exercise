using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManagement.API.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value is null)
                continue;

            var validatorType = typeof(IValidator<>)
                .MakeGenericType(argument.Value.GetType());

            var validator = context.HttpContext.RequestServices
                .GetService(validatorType) as IValidator;

            if (validator is null)
                continue;

            var validationContext = new ValidationContext<object>(
                argument.Value);

            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (result.IsValid)
                continue;

            foreach (var group in result.Errors.GroupBy(
                error => error.PropertyName))
            {
                errors[group.Key] = group
                    .Select(error => error.ErrorMessage)
                    .ToArray();
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed.",
                    Type = "https://httpstatuses.com/400"
                });

            return;
        }

        await next();
    }
}