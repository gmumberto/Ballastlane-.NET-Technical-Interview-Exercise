namespace TaskManagement.Application.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("Email is already registered.")
    {
    }

    public DuplicateEmailException(Exception innerException)
        : base("Email is already registered.", innerException)
    {
    }
}