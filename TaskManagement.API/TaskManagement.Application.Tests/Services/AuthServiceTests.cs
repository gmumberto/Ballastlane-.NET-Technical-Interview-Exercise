using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Common;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using Xunit;
using User = TaskManagement.Domain.Entities.User;

namespace TaskManagement.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        _service = new AuthService(
            _repositoryMock.Object,
            _jwtTokenGeneratorMock.Object,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = "test@example.com",
            Password = "StrongPassword@2026"
        };

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.HashPassword(
                It.IsAny<User>(),
                request.Password))
            .Returns("hashed_password");

        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(new AuthResponseDto
            {
                AccessToken = "valid_token",
                ExpiresIn = 3600
            });

        _repositoryMock.Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("valid_token", result.Value!.AccessToken);

        _repositoryMock.Verify(
            r => r.TryAddAsync(
                It.Is<User>(u =>
                    u.Email == request.Email &&
                    u.UserName == request.UserName &&
                    u.PasswordHash == "hashed_password"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _jwtTokenGeneratorMock.Verify(
            j => j.GenerateToken(It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ShouldThrow()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = "existing@example.com",
            Password = "StrongPassword@2026"
        };

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var result = await _service.RegisterAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);

        _repositoryMock.Verify(
            r => r.TryAddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("", "test@example.com", "StrongPassword@2026")]
    [InlineData("TestUser", "", "StrongPassword@2026")]
    [InlineData("TestUser", "test@example.com", "")]
    public async Task RegisterAsync_WithMissingFields_ShouldThrow(
        string userName,
        string email,
        string password)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            UserName = userName,
            Email = email,
            Password = password
        };

        // Act & Assert
        var result = await _service.RegisterAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);

        _repositoryMock.Verify(
            r => r.TryAddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ShouldThrow()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            UserName = "TestUser",
            Email = "test@example.com",
            Password = "123456"
        };

        // Act & Assert
        var result = await _service.RegisterAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);

        _repositoryMock.Verify(
            r => r.TryAddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var user = CreateUser();

        var request = new LoginRequestDto
        {
            Email = user.Email,
            Password = "StrongPassword@2026"
        };

        _repositoryMock
            .Setup(r => r.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password))
            .Returns(PasswordVerificationResult.Success);

        _jwtTokenGeneratorMock
            .Setup(j => j.GenerateToken(user))
            .Returns(new AuthResponseDto
            {
                AccessToken = "valid_jwt_token",
                ExpiresIn = 3600
            });

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("valid_jwt_token", result.Value!.AccessToken);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal(3600, result.Value.ExpiresIn);

        _jwtTokenGeneratorMock.Verify(
            j => j.GenerateToken(user),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrow()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "unknown@example.com",
            Password = "StrongPassword@2026"
        };

        _repositoryMock
            .Setup(r => r.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _service.LoginAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.Type);

        _jwtTokenGeneratorMock.Verify(
            j => j.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrow()
    {
        // Arrange
        var user = CreateUser();

        var request = new LoginRequestDto
        {
            Email = user.Email,
            Password = "WrongPassword@2026"
        };

        _repositoryMock
            .Setup(r => r.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password))
            .Returns(PasswordVerificationResult.Failed);

        // Act & Assert
        var result = await _service.LoginAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.Type);

        _jwtTokenGeneratorMock.Verify(
            j => j.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Theory]
    [InlineData("", "StrongPassword@2026")]
    [InlineData("test@example.com", "")]
    public async Task LoginAsync_WithMissingCredentials_ShouldThrow(
        string email,
        string password)
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act & Assert
        var result = await _service.LoginAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.Error!.Type);

        _jwtTokenGeneratorMock.Verify(
            j => j.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenUniqueConstraintWinsRace_ShouldReturnConflict()
    {
        var request = new RegisterRequestDto
        {
            UserName = "TestUser", Email = "race@example.com", Password = "StrongPassword@2026"
        };
        _repositoryMock.Setup(r => r.EmailExistsAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_password");
        _repositoryMock.Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.RegisterAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    private static User CreateUser()
    {
        return new User(
            "TestUser",
            "test@example.com",
            "hashed_password");
    }
}