
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public void LogAction_ShouldSaveAuditEntry()
    {
        var repository = new FakeAuditLogRepository();
        var service = CreateService(repository);

        service.LogAction(
            1,
            "Deactivate",
            "User",
            "2",
            "User account deactivated.");

        var entry = Assert.Single(repository.Entries);

        Assert.Equal(1, entry.UserId);
        Assert.Equal("Deactivate", entry.Action);
        Assert.Equal("User", entry.EntityType);
        Assert.Equal("2", entry.EntityId);
        Assert.Equal("User account deactivated.", entry.Details);
    }

    [Fact]
    public void LogAction_ShouldSetTimestamp()
    {
        var repository = new FakeAuditLogRepository();
        var service = CreateService(repository);

        service.LogAction(1, "Create", "User");

        var entry = Assert.Single(repository.Entries);

        Assert.NotEqual(default, entry.Timestamp);
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void LogAction_ShouldAllowNullUserId()
    {
        var repository = new FakeAuditLogRepository();
        var service = CreateService(repository);

        service.LogAction(null, "LoginFailed", "Authentication");

        var entry = Assert.Single(repository.Entries);

        Assert.Null(entry.UserId);
    }

    [Fact]
    public void LogAction_ShouldRejectEmptyAction()
    {
        var service = CreateService(
            new FakeAuditLogRepository());

        Assert.Throws<ArgumentException>(() =>
            service.LogAction(1, "", "User"));
    }

    [Fact]
    public void LogAction_ShouldRejectEmptyEntityType()
    {
        var service = CreateService(
            new FakeAuditLogRepository());

        Assert.Throws<ArgumentException>(() =>
            service.LogAction(1, "Update", ""));
    }

    [Fact]
    public void GetAllEntries_ShouldReturnAllEntries()
    {
        var repository = new FakeAuditLogRepository();

        repository.Add(new AuditLogEntry
        {
            Id = 1,
            Action = "Create",
            EntityType = "User"
        });

        repository.Add(new AuditLogEntry
        {
            Id = 2,
            Action = "Update",
            EntityType = "Product"
        });

        var service = CreateAuthorizedService(repository);

        var entries = service.GetAllEntries();

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void GetUserEntries_ShouldReturnEntriesForUser()
    {
        var repository = new FakeAuditLogRepository();

        repository.Add(new AuditLogEntry
        {
            Id = 1,
            UserId = 1,
            Action = "Create",
            EntityType = "User"
        });

        repository.Add(new AuditLogEntry
        {
            Id = 2,
            UserId = 2,
            Action = "Update",
            EntityType = "Product"
        });

        var service = CreateAuthorizedService(repository);

        var entries = service.GetUserEntries(1);

        Assert.Single(entries);
        Assert.Equal(1, entries[0].UserId);
    }

    [Fact]
    public void GetEntityEntries_ShouldReturnEntriesForEntity()
    {
        var repository = new FakeAuditLogRepository();

        repository.Add(new AuditLogEntry
        {
            Id = 1,
            Action = "Update",
            EntityType = "User",
            EntityId = "5"
        });

        repository.Add(new AuditLogEntry
        {
            Id = 2,
            Action = "Create",
            EntityType = "User",
            EntityId = "6"
        });

        repository.Add(new AuditLogEntry
        {
            Id = 3,
            Action = "Update",
            EntityType = "Product",
            EntityId = "5"
        });

        var service = CreateAuthorizedService(repository);

        var entries = service.GetEntityEntries("User", "5");

        Assert.Single(entries);
        Assert.Equal("5", entries[0].EntityId);
        Assert.Equal("User", entries[0].EntityType);
    }

    [Fact]
    public void GetAllEntries_WithoutPermission_ShouldThrowUnauthorizedAccessException()
    {
        var repository = new FakeAuditLogRepository();
        var service = CreateService(repository);

        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => service.GetAllEntries());

        Assert.Contains("permission", exception.Message);
    }

    private static AuditLogService CreateService(
        FakeAuditLogRepository repository)
    {
        return new AuditLogService(
            repository,
            new UserSessionService(),
            new AuthorizationService());
    }

    private static AuditLogService CreateAuthorizedService(
        FakeAuditLogRepository repository)
    {
        var userSession = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Auditor",
                        RolePermissions = new List<RolePermission>
                        {
                            new RolePermission
                            {
                                Permission = new Permission
                                {
                                    Name = "AuditLog.View"
                                }
                            }
                        }
                    }
                }
            }
        };

        userSession.StartSession(user);

        return new AuditLogService(
            repository,
            userSession,
            new AuthorizationService());
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
            return Entries
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        public List<AuditLogEntry> GetByUserId(int userId)
        {
            return Entries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
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
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        public void SaveChanges()
        {
            // No database is used in these tests.
        }
    }
}
