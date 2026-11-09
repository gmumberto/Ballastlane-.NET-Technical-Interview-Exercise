using TaskManagement.Application.DTOs;
using TaskManagement.Application.Validators;
using Xunit;

namespace TaskManagement.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Theory]
    [InlineData("password123456")]
    [InlineData("aaaaaaaaaaaa")]
    public async Task Validate_WithWeakPassword_ShouldFail(string password)
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser", Email = "test@example.com", Password = password
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithOverlongPassword_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser", Email = "test@example.com", Password = new string('a', 129)
        };
        var result = await _validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = "test@example.com",
            Password = "StrongPassword@2026"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    public async Task Validate_WithInvalidEmail_ShouldFail(
        string email)
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = email,
            Password = "StrongPassword@2026"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithShortPassword_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = "test@example.com",
            Password = "123456"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithEmptyUsername_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            UserName = "",
            Email = "test@example.com",
            Password = "StrongPassword@2026"
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "UserName");
    }
}