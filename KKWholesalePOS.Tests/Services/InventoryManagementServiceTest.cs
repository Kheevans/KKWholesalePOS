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

    [Fact]
    public void GetInventory_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.Receive");

        Assert.Throws<UnauthorizedAccessException>(
            () => service.GetInventory());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("  Rice  ", "Rice")]
    public void GetInventory_WithPermission_ReturnsInventoryRecords(
    string searchTerm, string expectedSearch)
    {
        var service = CreateAuthorizedService("Inventory.View");

        var products = new List<InventoryRow>
    {
        new InventoryRow
        {
            ProductId = 1,
            ProductName = "Rice",
            Barcode = "RICE-001",
            AvailableStock = 20,
            OutstandingConsignmentQuantity = 5,
            MinimumStockLevel = 10
        }
    };

        _repositoryMock
            .Setup(repository => repository.GetInventory(expectedSearch))
            .Returns(products);

        var result = service.GetInventory(searchTerm);

        var product = Assert.Single(result);

        Assert.Equal("Rice", product.ProductName);
        Assert.Equal(20, product.AvailableStock);
        Assert.Equal(5, product.OutstandingConsignmentQuantity);
        Assert.Equal("In Stock", product.Status);

        _repositoryMock.Verify(
            repository => repository.GetInventory(expectedSearch),
            Times.Once);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectInventoryTotals()
    {
        var service = CreateAuthorizedService("Inventory.View");

        var products = new List<InventoryRow>
    {
        new InventoryRow
        {
            ProductId = 1,
            AvailableStock = 20,
            OutstandingConsignmentQuantity = 5,
            MinimumStockLevel = 10
        },
        new InventoryRow
        {
            ProductId = 2,
            AvailableStock = 5,
            OutstandingConsignmentQuantity = 3,
            MinimumStockLevel = 5
        },
        new InventoryRow
        {
            ProductId = 3,
            AvailableStock = 0,
            MinimumStockLevel = 5
        },
        new InventoryRow
        {
            ProductId = 4,
            AvailableStock = -2,
            MinimumStockLevel = 5
        }
    };

        _repositoryMock
            .Setup(repository => repository.GetInventory(""))
            .Returns(products);

        var summary = service.GetSummary();

        Assert.Equal(4, summary.TotalProducts);
        Assert.Equal(3, summary.LowStockProductCount);
        Assert.Equal(1, summary.OutOfStockProductCount);
        Assert.Equal(1, summary.InvalidBalanceProductCount);
        Assert.Equal(23L, summary.TotalAvailableUnits);
        Assert.Equal(8L, summary.TotalOutstandingUnits);
    }

    [Fact]
    public void GetSummary_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        Assert.Throws<UnauthorizedAccessException>(
            () => service.GetSummary());
    }

    [Fact]
    public async Task ReceiveStockAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.View");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ReceiveStockAsync(1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ReceiveStockAsync_WithInvalidQuantity_ThrowsArgumentException(
        int quantity)
    {
        var service = CreateAuthorizedService("Inventory.Receive");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReceiveStockAsync(1, quantity));

        _repositoryMock.Verify(
            repository => repository.ReceiveStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0, "Stock count")]
    [InlineData(-5, "")]
    [InlineData(-5, "   ")]
    public async Task AdjustStockAsync_WithInvalidInput_ThrowsArgumentException(
    int quantity, string reason)
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AdjustStockAsync(1, quantity, reason));

        _repositoryMock.Verify(
            repository => repository.AdjustStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SetMinimumStockLevel_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.View");

        Assert.Throws<UnauthorizedAccessException>(
            () => service.SetMinimumStockLevel(1, 10));
    }

    [Fact]
    public void SetMinimumStockLevel_WithNegativeLevel_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        Assert.Throws<ArgumentException>(
            () => service.SetMinimumStockLevel(1, -5));

        _repositoryMock.Verify(
            repository => repository.UpdateMinimumStockLevel(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void SetMinimumStockLevel_WithValidLevel_UpdatesProductAndLogsAudit()
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        var product = new Product
        {
            Id = 1,
            Name = "Rice",
            MinimumStockLevel = 5
        };

        _repositoryMock
            .Setup(repository => repository.GetProductById(1))
            .Returns(product);

        service.SetMinimumStockLevel(1, 10);

        _repositoryMock.Verify(
            repository => repository.UpdateMinimumStockLevel(1, 10),
            Times.Once);

        _auditRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<AuditLogEntry>(entry =>
                    entry.Action == "MinimumStockLevelUpdated" &&
                    entry.EntityType == "Product" &&
                    entry.EntityId == "1")),
            Times.Once);
    }

    [Fact]
    public void SetMinimumStockLevel_WithMissingProduct_ThrowsInvalidOperationException()
    {
        var service = CreateAuthorizedService("Inventory.Adjust");

        _repositoryMock
            .Setup(repository => repository.GetProductById(1))
            .Returns((Product?)null);

        Assert.Throws<InvalidOperationException>(
            () => service.SetMinimumStockLevel(1, 10));
    }

    [Fact]
    public void GetStockMovementHistory_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Inventory.Receive");

        Assert.Throws<UnauthorizedAccessException>(
            () => service.GetStockMovementHistory(1));
    }

    [Fact]
    public void GetStockMovementHistory_WithPermission_ReturnsProductMovements()
    {
        var service = CreateAuthorizedService("Inventory.View");

        var movements = new List<StockMovementRow>
    {
        new StockMovementRow
        {
            MovementId = 1,
            ProductId = 1,
            ProductName = "Rice",
            QuantityChange = 20,
            MovementType = "StockReceived",
            UserId = 1,
            Username = "stockuser",
            RecordedAt = DateTime.UtcNow
        }
    };

        _repositoryMock
            .Setup(repository => repository.GetStockMovementHistory(1))
            .Returns(movements);

        var result = service.GetStockMovementHistory(1);

        var movement = Assert.Single(result);

        Assert.Equal(1, movement.ProductId);
        Assert.Equal(20, movement.QuantityChange);
        Assert.Equal("StockReceived", movement.MovementType);
        Assert.Equal("stockuser", movement.Username);
    }

    [Fact]
    public void GetStockMovementHistory_WithInvalidProductId_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService("Inventory.View");

        Assert.Throws<ArgumentException>(
            () => service.GetStockMovementHistory(0));
    }


}