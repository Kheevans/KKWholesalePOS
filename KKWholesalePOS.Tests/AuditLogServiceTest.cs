using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public void LogAction_ShouldSaveAuditEntry()
    {
        // Arrange
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        // Act
        service.LogAction(
            1,
            "Deactivate",
            "User",
            "2",
            "User account deactivated.");

        // Assert
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
        // Arrange
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        // Act
        service.LogAction(1, "Create", "User");

        // Assert
        var entry = Assert.Single(repository.Entries);

        Assert.NotEqual(default, entry.Timestamp);
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void LogAction_ShouldAllowNullUserId()
    {
        // Arrange
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);

        // Act
        service.LogAction(null, "LoginFailed", "Authentication");

        // Assert
        var entry = Assert.Single(repository.Entries);

        Assert.Null(entry.UserId);
    }

    [Fact]
    public void LogAction_ShouldRejectEmptyAction()
    {
        // Arrange
        var service = new AuditLogService(
            new FakeAuditLogRepository());

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            service.LogAction(1, "", "User"));
    }

    [Fact]
    public void LogAction_ShouldRejectEmptyEntityType()
    {
        // Arrange
        var service = new AuditLogService(
            new FakeAuditLogRepository());

        // Act and Assert
        Assert.Throws<ArgumentException>(() =>
            service.LogAction(1, "Update", ""));
    }

    [Fact]
    public void GetAllEntries_ShouldReturnAllEntries()
    {
        // Arrange
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

        var service = new AuditLogService(repository);

        // Act
        var entries = service.GetAllEntries();

        // Assert
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void GetUserEntries_ShouldReturnEntriesForUser()
    {
        // Arrange
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

        var service = new AuditLogService(repository);

        // Act
        var entries = service.GetUserEntries(1);

        // Assert
        Assert.Single(entries);
        Assert.Equal(1, entries[0].UserId);
    }

    [Fact]
    public void GetEntityEntries_ShouldReturnEntriesForEntity()
    {
        // Arrange
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

        var service = new AuditLogService(repository);

        // Act
        var entries = service.GetEntityEntries("User", "5");

        // Assert
        Assert.Single(entries);
        Assert.Equal("5", entries[0].EntityId);
        Assert.Equal("User", entries[0].EntityType);
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
