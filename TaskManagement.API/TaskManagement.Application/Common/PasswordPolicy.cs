namespace TaskManagement.Application.Common;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password123456", "123456789012", "qwerty123456", "admin12345678",
        "letmein123456", "passwordpassword", "welcome123456"
    };

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return "Password is required.";
        if (password.Length < MinimumLength || password.Length > MaximumLength)
            return $"Password must contain between {MinimumLength} and {MaximumLength} characters.";
        if (CommonPasswords.Contains(password) || password.Trim().Distinct().Count() == 1)
            return "Password is too common or easily guessed.";
        return null;
    }
}
