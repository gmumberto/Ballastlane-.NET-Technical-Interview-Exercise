namespace TaskManagement.Application.Common;

public enum ErrorType { Validation, NotFound, Conflict, Unauthorized }

public sealed record AppError(ErrorType Type, string Code, string Message);

public sealed class Result<T>
{
    private Result(T? value, AppError? error) { Value = value; Error = error; }
    public T? Value { get; }
    public AppError? Error { get; }
    public bool IsSuccess => Error is null;
    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(AppError error) => new(default, error);
}
