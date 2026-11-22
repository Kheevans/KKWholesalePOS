using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService = new();

    [Fact]
    public void HashPassword_ShouldNotReturnPlainTextPassword()
    {
        // Arrange
        var user = new User();
        string password = "TestPassword123!";

        // Act
        string hashedPassword = _passwordService.HashPassword(
            user,
            password);

        // Assert
        Assert.NotEqual(password, hashedPassword);
        Assert.NotEmpty(hashedPassword);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        var user = new User();
        string password = "TestPassword123!";

        user.PasswordHash = _passwordService.HashPassword(
            user,
            password);

        // Act
        bool result = _passwordService.VerifyPassword(
            user,
            password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        var user = new User();

        user.PasswordHash = _passwordService.HashPassword(
            user,
            "TestPassword123!");

        // Act
        bool result = _passwordService.VerifyPassword(
            user,
            "WrongPassword123!");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_ShouldThrowExceptionForEmptyPassword()
    {
        // Arrange
        var user = new User();

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            _passwordService.HashPassword(user, ""));
    }
}
