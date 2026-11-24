
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class UserManagementServiceTests
{
    private readonly PasswordService _passwordService = new();

    private UserManagementService CreateService(
            FakeUserRepository repository)
    {
        var userSession = CreateAuthorizedSession("Users.Manage");
        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            new Moq.Mock<IAuditLogRepository>().Object,
            userSession,
            authService);

        return new UserManagementService(
            repository,
            _passwordService,
            userSession,
            authService,
            auditService);
    }

    private UserManagementService CreateUnauthorizedService(
        FakeUserRepository repository)
    {
        var userSession = new UserSessionService();
        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            new Moq.Mock<IAuditLogRepository>().Object,
            userSession,
            authService);

        return new UserManagementService(
            repository,
            _passwordService,
            userSession,
            authService,
            auditService);
    }

    private static UserSessionService CreateAuthorizedSession(
        string permissionName)
    {
        var userSession = new UserSessionService();

        var user = new User
        {
            Id = 100,
            Username = "testadmin",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Administrator",
                        RolePermissions = new List<RolePermission>
                        {
                            new RolePermission
                            {
                                Permission = new Permission
                                {
                                    Name = permissionName
                                }
                            }
                        }
                    }
                }
            }
        };

        userSession.StartSession(user);

        return userSession;
    }

    [Fact]
    public void CreateUser_ShouldSaveUserWithHashedPassword()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var user = service.CreateUser(
            "admin",
            "AdminPassword123!");

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
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        service.CreateUser("admin", "AdminPassword123!");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateUser("admin", "AnotherPassword123!"));
    }

    [Fact]
    public void CreateUser_ShouldRejectEmptyUsername()
    {
        var service = CreateService(new FakeUserRepository());

        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("", "AdminPassword123!"));
    }

    [Fact]
    public void CreateUser_ShouldRejectShortPassword()
    {
        var service = CreateService(new FakeUserRepository());

        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("admin", "short"));
    }

    [Fact]
    public void DeactivateUser_ShouldSetAccountInactive()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var user = service.CreateUser(
            "admin", "AdminPassword123!");

        service.DeactivateUser(user.Id);

        Assert.False(user.IsActive);
    }

    [Fact]
    public void ActivateUser_ShouldSetAccountActive()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var user = service.CreateUser(
            "admin", "AdminPassword123!");

        user.IsActive = false;

        service.ActivateUser(user.Id);

        Assert.True(user.IsActive);
    }

    [Fact]
    public void DeactivateUser_ShouldRejectUnknownUser()
    {
        var service = CreateService(new FakeUserRepository());

        Assert.Throws<InvalidOperationException>(() =>
            service.DeactivateUser(999));
    }

    [Fact]
    public void CreateUser_WithoutPermission_ShouldThrowUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService(
            new FakeUserRepository());

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.CreateUser("admin", "AdminPassword123!"));
    }

    [Fact]
    public void GetAllUsers_ShouldReturnAllUsers()
    {
        var repository = new FakeUserRepository();

        repository.Add(new User
        {
            Username = "admin",
            IsActive = true
        });

        repository.Add(new User
        {
            Username = "cashier",
            IsActive = true
        });

        var service = CreateService(repository);

        var users = service.GetAllUsers();

        Assert.Equal(2, users.Count);
    }

    private class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        private int _nextId = 1;

        public List<User> GetAll()
        {
            return Users;
        }

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
