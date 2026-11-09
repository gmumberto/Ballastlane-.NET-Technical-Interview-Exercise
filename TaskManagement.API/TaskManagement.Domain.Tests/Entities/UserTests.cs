using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        var user = new User(
            "testuser",
            "test@example.com",
            "hashed-password");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("testuser", user.UserName);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidUsername_ShouldThrow(
        string? username)
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                username!,
                "test@example.com",
                "hashed-password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidEmail_ShouldThrow(
        string? email)
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                "testuser",
                email!,
                "hashed-password"));
    }

    [Fact]
    public void Constructor_WithEmptyPasswordHash_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(
                "testuser",
                "test@example.com",
                ""));
    }


    [Fact]
    public void SetPasswordHash_WithValidHash_ShouldUpdatePasswordHash()
    {
        // Arrange
        var user = new User(
            "TestUser",
            "test@example.com",
            "original_hash");

        // Act
        user.SetPasswordHash("new_secure_hash");

        // Assert
        Assert.Equal("new_secure_hash", user.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetPasswordHash_WithInvalidHash_ShouldThrow(
        string? passwordHash)
    {
        // Arrange
        var user = new User(
            "TestUser",
            "test@example.com",
            "original_hash");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => user.SetPasswordHash(passwordHash!));

        Assert.Equal("original_hash", user.PasswordHash);
    }

}
