using Microsoft.AspNetCore.Identity;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using User = TaskManagement.Domain.Entities.User;

namespace TaskManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _tokens;
    private readonly IPasswordHasher<User> _hasher;
    public AuthService(IUserRepository users, IJwtTokenGenerator tokens, IPasswordHasher<User> hasher)
    { _users = users; _tokens = tokens; _hasher = hasher; }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
            return Result<AuthResponseDto>.Failure(new(ErrorType.Validation, "Auth.RequiredFields", "Username and email are required."));
        var passwordError = PasswordPolicy.Validate(request.Password);
        if (passwordError is not null)
            return Result<AuthResponseDto>.Failure(new(ErrorType.Validation, "Auth.InvalidPassword", passwordError));
        var email = request.Email.Trim();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return Result<AuthResponseDto>.Failure(new(ErrorType.Conflict, "Auth.DuplicateEmail", "Email is already registered."));
        var user = new User(request.UserName, email, "PENDING_HASH");
        user.SetPasswordHash(_hasher.HashPassword(user, request.Password));
        // The repository translates unique-key conflicts into a typed result to handle registration races.
        var saved = await _users.TryAddAsync(user, cancellationToken);
        if (!saved)
            return Result<AuthResponseDto>.Failure(new(ErrorType.Conflict, "Auth.DuplicateEmail", "Email is already registered."));
        return Result<AuthResponseDto>.Success(_tokens.GenerateToken(user));
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var invalid = new AppError(ErrorType.Unauthorized, "Auth.InvalidCredentials", "Invalid credentials.");
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResponseDto>.Failure(invalid);
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null) return Result<AuthResponseDto>.Failure(invalid);
        var verified = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verified == PasswordVerificationResult.Failed)
            return Result<AuthResponseDto>.Failure(invalid);
        return Result<AuthResponseDto>.Success(_tokens.GenerateToken(user));
    }
}
