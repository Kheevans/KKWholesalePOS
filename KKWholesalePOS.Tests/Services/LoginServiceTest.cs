using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class LoginServiceTests
{
    [Fact]
    public void Login_ShouldStartSessionForValidCredentials()
    {
        // Arrange
        var user = CreateTestUser();

        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var passwordService = new PasswordService();
        user.PasswordHash = passwordService.HashPassword(
            user,
            "Password123");

        var authenticationService = new AuthenticationService(
            userRepository,
            passwordService);

        var session = new UserSessionService();
        var auditRepository = new FakeAuditLogRepository();
        var auditService = new AuditLogService(auditRepository);

        var loginService = new LoginService(
            authenticationService,
            session,
            auditService);

        // Act
        bool result = loginService.Login(
            "testuser",
            "Password123");

        // Assert
        Assert.True(result);
        Assert.True(session.IsAuthenticated);
        Assert.Equal(user.Id, session.CurrentUser!.Id);

        Assert.Contains(
            auditRepository.Entries,
            e => e.Action == "Login");
    }

    [Fact]
    public void Login_ShouldNotStartSessionForInvalidCredentials()
    {
        // Arrange
        var user = CreateTestUser();

        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var passwordService = new PasswordService();
        user.PasswordHash = passwordService.HashPassword(
            user,
            "Password123");

        var authenticationService = new AuthenticationService(
            userRepository,
            passwordService);

        var session = new UserSessionService();
        var auditRepository = new FakeAuditLogRepository();
        var auditService = new AuditLogService(auditRepository);

        var loginService = new LoginService(
            authenticationService,
            session,
            auditService);

        // Act
        bool result = loginService.Login(
            "testuser",
            "WrongPassword");

        // Assert
        Assert.False(result);
        Assert.False(session.IsAuthenticated);

        Assert.Contains(
            auditRepository.Entries,
            e => e.Action == "LoginFailed");
    }

    [Fact]
    public void Logout_ShouldEndSessionAndRecordAction()
    {
        // Arrange
        var user = CreateTestUser();

        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var passwordService = new PasswordService();
        user.PasswordHash = passwordService.HashPassword(
            user,
            "Password123");

        var authenticationService = new AuthenticationService(
            userRepository,
            passwordService);

        var session = new UserSessionService();
        session.StartSession(user);

        var auditRepository = new FakeAuditLogRepository();
        var auditService = new AuditLogService(auditRepository);

        var loginService = new LoginService(
            authenticationService,
            session,
            auditService);

        // Act
        loginService.Logout();

        // Assert
        Assert.False(session.IsAuthenticated);

        Assert.Contains(
            auditRepository.Entries,
            e => e.Action == "Logout");
    }

    [Fact]
    public void Logout_ShouldDoNothingWhenNoUserIsLoggedIn()
    {
        // Arrange
        var userRepository = new FakeUserRepository();
        var passwordService = new PasswordService();

        var authenticationService = new AuthenticationService(
            userRepository,
            passwordService);

        var session = new UserSessionService();
        var auditRepository = new FakeAuditLogRepository();
        var auditService = new AuditLogService(auditRepository);

        var loginService = new LoginService(
            authenticationService,
            session,
            auditService);

        // Act
        loginService.Logout();

        // Assert
        Assert.False(session.IsAuthenticated);
        Assert.Empty(auditRepository.Entries);
    }

    private static User CreateTestUser()
    {
        return new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true
        };
    }

    private class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public User? GetByUsername(string username)
        {
            return _users.FirstOrDefault(
                u => u.Username == username);
        }

        public User? GetById(int userId)
        {
            return _users.FirstOrDefault(
                u => u.Id == userId);
        }

        public bool HasAnyUsers()
        {
            return _users.Any();
        }

        public void Add(User user)
        {
            _users.Add(user);
        }

        public void Update(User user)
        {
            // Objects are stored by reference in this fake repository.
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
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

    private class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLogEntry> Entries { get; } = new();

        public void Add(AuditLogEntry entry)
        {
            entry.Id = Entries.Count + 1;
            Entries.Add(entry);
        }

        public List<AuditLogEntry> GetAll()
        {
            return Entries;
        }

        public List<AuditLogEntry> GetByUserId(int userId)
        {
            return Entries
                .Where(e => e.UserId == userId)
                .ToList();
        }

        public List<AuditLogEntry> GetByEntity(
            string entityType,
            string entityId)
        {
            return Entries
                .Where(e =>
                    e.EntityType == entityType &&
                    e.EntityId == entityId)
                .ToList();
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
        }
    }
}
