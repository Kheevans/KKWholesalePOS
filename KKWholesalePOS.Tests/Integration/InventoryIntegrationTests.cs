using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using KKWholesalePOS.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Tests;

public class InventoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;
    private readonly InventoryRepository _repository;

    public InventoryIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _inventoryService = new InventoryService(_context);
        _repository = new InventoryRepository(
            _context, _inventoryService);

        // Add test user, customer and product
        _context.Users.Add(new User
        {
            Id = 1,
            Username = "inventoryuser",
            PasswordHash = "test-hash",
            IsActive = true
        });

        _context.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Test Customer"
        });

        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Rice",
            Barcode = "RICE-001",
            SellingPrice = 500m,
            StockQuantity = 20,
            MinimumStockLevel = 5,
            UnitsPerCase = 12
        });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private InventoryRow GetInventoryProduct()
    {
        return Assert.Single(_repository.GetInventory(""));
    }

    private ConsignmentLine IssueConsignment(int quantity)
    {
        using var transaction = _context.Database.BeginTransaction();

        var consignment = new Consignment
        {
            CustomerId = 1,
            UserId = 1,
            ReferenceNumber = $"CON-TEST-{Guid.NewGuid():N}",
            OriginalGoodsValue = quantity * 500m,
            ConsignmentFee = quantity * 500m * 0.05m
        };

        _context.Consignments.Add(consignment);
        _context.SaveChanges();

        var line = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = 1,
            ProductName = "Rice",
            Barcode = "RICE-001",
            UnitPrice = 500m,
            Quantity = quantity,
            LineTotal = quantity * 500m
        };

        _context.ConsignmentLines.Add(line);
        _context.SaveChanges();

        _inventoryService.ApplyStockChange(
            productId: 1,
            quantityChange: -quantity,
            userId: 1,
            movementType: InventoryService.ConsignmentIssued,
            consignmentId: consignment.Id);

        _context.SaveChanges();
        transaction.Commit();

        return line;
    }

    private void ReturnConsignment(
        ConsignmentLine line,
        int quantityReturned,
        int quantityAccepted)
    {
        using var transaction = _context.Database.BeginTransaction();

        var consignmentReturn = new ConsignmentReturn
        {
            ConsignmentId = line.ConsignmentId,
            UserId = 1,
            ReferenceNumber = $"RET-TEST-{Guid.NewGuid():N}",
            ReturnDate = DateTime.UtcNow
        };

        _context.ConsignmentReturns.Add(consignmentReturn);
        _context.SaveChanges();

        _context.ConsignmentReturnLines.Add(new ConsignmentReturnLine
        {
            ConsignmentReturnId = consignmentReturn.Id,
            ConsignmentLineId = line.Id,
            QuantityReturned = quantityReturned,
            QuantityAccepted = quantityAccepted,
            UnitPrice = line.UnitPrice
        });

        // Only accepted returns increase available stock
        if (quantityAccepted > 0)
        {
            _inventoryService.ApplyStockChange(
                productId: line.ProductId,
                quantityChange: quantityAccepted,
                userId: 1,
                movementType: InventoryService.ConsignmentReturn,
                consignmentReturnId: consignmentReturn.Id);
        }

        _context.SaveChanges();
        transaction.Commit();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void IssueConsignment_UpdatesStockAndRecordsMovement()
    {
        var line = IssueConsignment(8);

        var product = GetInventoryProduct();

        Assert.Equal(12, product.AvailableStock);
        Assert.Equal(8, product.OutstandingConsignmentQuantity);
        Assert.False(product.HasInvalidBalance);

        var movement = Assert.Single(
            _context.StockMovements.AsNoTracking().ToList());

        Assert.Equal(1, movement.ProductId);
        Assert.Equal(-8, movement.QuantityChange);
        Assert.Equal(InventoryService.ConsignmentIssued, movement.MovementType);
        Assert.Equal(line.ConsignmentId, movement.ConsignmentId);
        Assert.Equal(1, movement.UserId);
        Assert.NotEqual(default(DateTime), movement.RecordedAt);
    }

    [Fact]
    public void GetInventory_ReturnsTotalOutstandingForMultipleConsignments()
    {
        IssueConsignment(5);
        IssueConsignment(3);

        var product = GetInventoryProduct();

        Assert.Equal(12, product.AvailableStock);
        Assert.Equal(8, product.OutstandingConsignmentQuantity);
    }

    [Theory]
    [InlineData(4, 4, 16, 4)]
    [InlineData(4, 0, 12, 8)]
    [InlineData(4, 2, 14, 6)]
    [InlineData(8, 8, 20, 0)]
    public void ReturnConsignment_UpdatesStockForAcceptedReturns(
        int quantityReturned,
        int quantityAccepted,
        int expectedAvailable,
        int expectedOutstanding)
    {
        var line = IssueConsignment(8);

        ReturnConsignment(line, quantityReturned, quantityAccepted);

        var product = GetInventoryProduct();

        Assert.Equal(expectedAvailable, product.AvailableStock);
        Assert.Equal(expectedOutstanding, product.OutstandingConsignmentQuantity);
        Assert.False(product.HasInvalidBalance);

        var movements = _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovementType == InventoryService.ConsignmentReturn)
            .ToList();

        if (quantityAccepted > 0)
        {
            var movement = Assert.Single(movements);

            Assert.Equal(1, movement.ProductId);
            Assert.Equal(quantityAccepted, movement.QuantityChange);
            Assert.Equal(1, movement.UserId);
            Assert.NotNull(movement.ConsignmentReturnId);
            Assert.NotEqual(default(DateTime), movement.RecordedAt);
        }
        else
        {
            Assert.Empty(movements);
        }
    }

    [Fact]
    public void IssueConsignment_RollsBackWhenStockIsInsufficient()
    {
        Assert.Throws<InvalidOperationException>(
            () => IssueConsignment(21));

        _context.ChangeTracker.Clear();

        var product = GetInventoryProduct();

        Assert.Equal(20, product.AvailableStock);
        Assert.Equal(0, product.OutstandingConsignmentQuantity);

        Assert.Empty(_context.Consignments.ToList());
        Assert.Empty(_context.ConsignmentLines.ToList());
        Assert.Empty(_context.StockMovements.ToList());
    }

    [Fact]
    public void GetInventory_FlagsNegativeOutstandingStock()
    {
        var line = IssueConsignment(5);

        // Simulate an existing invalid return record
        ReturnConsignment(line, 6, 6);

        var product = GetInventoryProduct();

        Assert.Equal(-1, product.OutstandingConsignmentQuantity);
        Assert.True(product.HasInvalidBalance);
        Assert.Equal("Invalid Balance", product.Status);
    }

    [Fact]
    public void GetInventory_FlagsInvalidConsignmentLine()
    {
        var firstLine = IssueConsignment(5);

        IssueConsignment(4);

        // A positive total should not hide an invalid consignment line
        ReturnConsignment(firstLine, 6, 6);

        var product = GetInventoryProduct();

        Assert.Equal(3, product.OutstandingConsignmentQuantity);
        Assert.True(product.HasInvalidConsignmentBalance);
        Assert.True(product.HasInvalidBalance);
        Assert.Equal("Invalid Balance", product.Status);
    }

    [Fact]
    public async Task ReceiveStock_PreservesOutstandingConsignmentStock()
    {
        IssueConsignment(8);

        await _repository.ReceiveStockAsync(
            1, 10, 1, "Supplier delivery");

        var product = GetInventoryProduct();

        Assert.Equal(22, product.AvailableStock);
        Assert.Equal(8, product.OutstandingConsignmentQuantity);
    }

    [Fact]
    public async Task AdjustStock_PreservesOutstandingConsignmentStock()
    {
        IssueConsignment(8);

        await _repository.AdjustStockAsync(
            1, -2, 1, "Damaged items");

        var product = GetInventoryProduct();

        Assert.Equal(10, product.AvailableStock);
        Assert.Equal(8, product.OutstandingConsignmentQuantity);
    }

    [Fact]
    public void IssueConsignment_FlagsLowStockAtMinimumLevel()
    {
        IssueConsignment(15);

        var product = GetInventoryProduct();

        Assert.Equal(5, product.AvailableStock);
        Assert.Equal(15, product.OutstandingConsignmentQuantity);
        Assert.True(product.IsLowStock);
        Assert.Equal("Low Stock", product.Status);
    }
}