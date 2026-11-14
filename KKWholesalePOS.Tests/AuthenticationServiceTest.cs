using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class AuthenticationServiceTests
{
    private readonly PasswordService _passwordService = new();

    private AuthenticationService CreateService(User? user)
    {
        var repository = new FakeUserRepository(user);

        return new AuthenticationService(
            repository,
            _passwordService);
    }

    [Fact]
    public void Authenticate_ShouldReturnUserForValidCredentials()
    {
        // Arrange
        var user = new User
        {
            Username = "admin",
            IsActive = true
        };

        user.PasswordHash = _passwordService.HashPassword(
            user,
            "AdminPassword123!");

        var service = CreateService(user);

        // Act
        var result = service.Authenticate(
            "admin",
            "AdminPassword123!");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public void Authenticate_ShouldReturnNullForIncorrectPassword()
    {
        // Arrange
        var user = CreateUser();
        var service = CreateService(user);

        // Act
        var result = service.Authenticate(
            "admin",
            "WrongPassword123!");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Authenticate_ShouldReturnNullForInactiveUser()
    {
        // Arrange
        var user = CreateUser();
        user.IsActive = false;

        var service = CreateService(user);

        // Act
        var result = service.Authenticate(
            "admin",
            "AdminPassword123!");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Authenticate_ShouldReturnNullForUnknownUsername()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var result = service.Authenticate(
            "unknown",
            "AdminPassword123!");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Authenticate_ShouldReturnNullForEmptyCredentials()
    {
        // Arrange
        var service = CreateService(CreateUser());

        // Act
        var result = service.Authenticate("", "");

        // Assert
        Assert.Null(result);
    }

    private User CreateUser()
    {
        var user = new User
        {
            Username = "admin",
            IsActive = true
        };

        user.PasswordHash = _passwordService.HashPassword(
            user,
            "AdminPassword123!");

        return user;
    }

    private class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user)
        {
            _user = user;
        }

        public User? GetByUsername(string username)
        {
            if (_user == null ||
                _user.Username != username)
            {
                return null;
            }

            return _user;
        }

        public User? GetById(int userId)
        {
            if (_user == null || _user.Id != userId)
            {
                return null;
            }

            return _user;
        }

        public bool HasAnyUsers()
        {
            return _user != null;
        }

        public void Add(User user)
        {
            throw new NotImplementedException();
        }

        public void Update(User user)
        {
            throw new NotImplementedException();
        }

        public void SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void AssignRole(int userId, int roleId)
        {
            throw new NotImplementedException();
        }

        public void RemoveRole(int userId, int roleId)
        {
            throw new NotImplementedException();
        }

        public List<Role> GetUserRoles(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
