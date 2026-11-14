
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class UserManagementServiceTests
{
    private readonly PasswordService _passwordService = new();

    private UserManagementService CreateService(
        FakeUserRepository repository)
    {
        return new UserManagementService(
            repository,
            _passwordService);
    }

    [Fact]
    public void CreateUser_ShouldSaveUserWithHashedPassword()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        // Act
        var user = service.CreateUser(
            "admin",
            "AdminPassword123!");

        // Assert
        Assert.Equal("admin", user.Username);
        Assert.True(user.IsActive);
        Assert.NotEqual("AdminPassword123!", user.PasswordHash);
        Assert.True(_passwordService.VerifyPassword(
            user, "AdminPassword123!"));
        Assert.Single(repository.Users);
    }

    [Fact]
    public void CreateUser_ShouldRejectDuplicateUsername()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        service.CreateUser("admin", "AdminPassword123!");

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.CreateUser("admin", "AnotherPassword123!"));
    }

    [Fact]
    public void CreateUser_ShouldRejectEmptyUsername()
    {
        // Arrange
        var service = CreateService(new FakeUserRepository());

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("", "AdminPassword123!"));
    }

    [Fact]
    public void CreateUser_ShouldRejectShortPassword()
    {
        // Arrange
        var service = CreateService(new FakeUserRepository());

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("admin", "short"));
    }

    [Fact]
    public void DeactivateUser_ShouldSetAccountInactive()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var user = service.CreateUser(
            "admin", "AdminPassword123!");

        // Act
        service.DeactivateUser(user.Id);

        // Assert
        Assert.False(user.IsActive);
    }

    [Fact]
    public void ActivateUser_ShouldSetAccountActive()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var user = service.CreateUser(
            "admin", "AdminPassword123!");

        user.IsActive = false;

        // Act
        service.ActivateUser(user.Id);

        // Assert
        Assert.True(user.IsActive);
    }

    [Fact]
    public void DeactivateUser_ShouldRejectUnknownUser()
    {
        // Arrange
        var service = CreateService(new FakeUserRepository());

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.DeactivateUser(999));
    }

    private class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        private int _nextId = 1;

        public User? GetByUsername(string username)
        {
            return Users.FirstOrDefault(
                u => u.Username == username);
        }

        public User? GetById(int userId)
        {
            return Users.FirstOrDefault(
                u => u.Id == userId);
        }

        public bool HasAnyUsers()
        {
            return Users.Any();
        }

        public void Add(User user)
        {
            user.Id = _nextId++;
            Users.Add(user);
        }

        public void Update(User user)
        {
            // The fake repository stores references
            // to the same user objects.
        }

        public void SaveChanges()
        {
            // No database is required for these tests.
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
