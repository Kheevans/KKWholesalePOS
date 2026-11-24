using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;
using Moq;

namespace KKWholesalePOS.Tests.Services;

public class InventoryManagementServiceTests
{
    private readonly Mock<IInventoryRepository> _repositoryMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepositoryMock = new();

    private InventoryManagementService CreateAuthorizedService(params string[] permissions)
    {
        var session = new UserSessionService();

        var rolePermissions = permissions
            .Select(p => new RolePermission
            {
                Permission = new Permission { Name = p }
            })
            .ToList();

        var user = new User
        {
            Id = 1,
            Username = "stockuser",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "StockRole",
                        RolePermissions = rolePermissions
                    }
                }
            }
        };

        session.StartSession(user);

        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            _auditRepositoryMock.Object,
            session,
            authService);

        return new InventoryManagementService(
            _repositoryMock.Object,
            session,
            authService,
            auditService);
    }

    [Fact]
    public void GetAllProducts_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetAllProducts());
    }

    [Fact]
    public async Task ReceiveStockAsync_WithPermission_CallsRepositoryAndLogsAudit()
    {
        var service = CreateAuthorizedService("Inventory.Receive");

        await service.ReceiveStockAsync(1, 20, "Weekly delivery");

        _repositoryMock.Verify(
            r => r.ReceiveStockAsync(1, 20, 1, "Weekly delivery"),
            Times.Once);

        _auditRepositoryMock.Verify(
            a => a.Add(It.Is<AuditLogEntry>(e =>
                e.Action == "StockReceived" &&
                e.EntityType == "Product" &&
                e.EntityId == "1")),
            Times.Once);
    }

    [Fact]
    public async Task AdjustStockAsync_WithoutAdjustPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.View");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AdjustStockAsync(1, -2, "Damaged"));
    }

    [Fact]
    public async Task AdjustStockAsync_WithPermissionAndReason_CallsRepositoryAndLogsAudit()
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        await service.AdjustStockAsync(1, -2, "Broken bottles");

        _repositoryMock.Verify(
            r => r.AdjustStockAsync(1, -2, 1, "Broken bottles"),
            Times.Once);

        _auditRepositoryMock.Verify(
            a => a.Add(It.Is<AuditLogEntry>(e =>
                e.Action == "StockAdjusted" &&
                e.Details!.Contains("Broken bottles"))),
            Times.Once);
    }
}