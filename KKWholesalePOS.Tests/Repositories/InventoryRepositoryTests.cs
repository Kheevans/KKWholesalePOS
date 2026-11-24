using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using KKWholesalePOS.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Tests;

public class InventoryRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly InventoryRepository _repository;

    public InventoryRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var inventoryService = new InventoryService(_context);

        _repository = new InventoryRepository(
            _context, inventoryService);

        // Add test user and products
        _context.Users.Add(new User
        {
            Id = 1,
            Username = "inventoryuser",
            PasswordHash = "test-hash",
            IsActive = true
        });

        _context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Rice",
                Barcode = "RICE-001",
                SellingPrice = 100m,
                StockQuantity = 10,
                UnitsPerCase = 12,
                MinimumStockLevel = 5
            },
            new Product
            {
                Id = 2,
                Name = "Sugar",
                Barcode = "SUGAR-001",
                SellingPrice = 150m,
                StockQuantity = 5,
                UnitsPerCase = 24,
                MinimumStockLevel = 5
            });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private int GetStockQuantity(int productId)
    {
        return _context.Products
            .AsNoTracking()
            .Single(p => p.Id == productId)
            .StockQuantity;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void GetAllProducts_ReturnsAllProducts()
    {
        var products = _repository.GetAllProducts();

        Assert.Equal(2, products.Count);
        Assert.Equal("Rice", products[0].Name);
        Assert.Equal("Sugar", products[1].Name);
    }

    [Fact]
    public void GetProductById_ReturnsMatchingProduct()
    {
        var product = _repository.GetProductById(1);

        Assert.NotNull(product);
        Assert.Equal("Rice", product.Name);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void GetProductByBarcode_ReturnsMatchingProduct()
    {
        var product = _repository.GetProductByBarcode("SUGAR-001");

        Assert.NotNull(product);
        Assert.Equal("Sugar", product.Name);
    }

    [Theory]
    [InlineData("Rice")]
    [InlineData("RICE")]
    [InlineData("ric")]
    [InlineData("  Rice  ")]
    [InlineData("RICE-001")]
    public void SearchProducts_ReturnsMatchingProduct(string query)
    {
        var products = _repository.SearchProducts(query);

        var product = Assert.Single(products);

        Assert.Equal(1, product.Id);
        Assert.Equal("Rice", product.Name);
    }

    [Fact]
    public void SearchProducts_ReturnsEmptyListWhenNoMatch()
    {
        var products = _repository.SearchProducts("Unknown");

        Assert.Empty(products);
    }

    [Fact]
    public void GetInventory_ReturnsProductDetailsAndStockQuantities()
    {
        var products = _repository.GetInventory("");

        Assert.Equal(2, products.Count);

        var product = products.Single(p => p.ProductId == 1);

        Assert.Equal("Rice", product.ProductName);
        Assert.Equal("RICE-001", product.Barcode);
        Assert.Equal(100m, product.SellingPrice);
        Assert.Equal(12, product.UnitsPerCase);
        Assert.Equal(10, product.AvailableStock);
        Assert.Equal(0, product.OutstandingConsignmentQuantity);
        Assert.Equal(5, product.MinimumStockLevel);
        Assert.Equal("In Stock", product.Status);
    }

    [Theory]
    [InlineData("Sugar")]
    [InlineData("SUGAR-001")]
    public void GetInventory_ReturnsMatchingProduct(string searchTerm)
    {
        var products = _repository.GetInventory(searchTerm);

        var product = Assert.Single(products);

        Assert.Equal(2, product.ProductId);
        Assert.Equal("Sugar", product.ProductName);
    }

    [Fact]
    public void GetInventory_ReturnsEmptyListWhenNoMatch()
    {
        var products = _repository.GetInventory("Unknown");

        Assert.Empty(products);
    }

    [Fact]
    public void UpdateMinimumStockLevel_UpdatesProductMinimum()
    {
        _repository.UpdateMinimumStockLevel(1, 15);

        _context.ChangeTracker.Clear();

        var product = _repository.GetProductById(1);

        Assert.NotNull(product);
        Assert.Equal(15, product.MinimumStockLevel);
        Assert.Equal(10, product.StockQuantity);
        Assert.Empty(_context.StockMovements.ToList());
    }

    [Fact]
    public void UpdateMinimumStockLevel_RejectsNonexistentProduct()
    {
        Assert.Throws<InvalidOperationException>(
            () => _repository.UpdateMinimumStockLevel(999, 10));
    }

    [Fact]
    public void GetLowStockProducts_IncludesProductAtMinimumLevel()
    {
        var products = _repository.GetLowStockProducts();

        var product = Assert.Single(products);

        Assert.Equal(2, product.Id);
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(5, product.MinimumStockLevel);
    }

    [Fact]
    public void GetInventory_IdentifiesLowStockAfterMinimumLevelChange()
    {
        _repository.UpdateMinimumStockLevel(1, 15);

        var products = _repository.GetInventory("");

        var product = products.Single(p => p.ProductId == 1);

        Assert.True(product.IsLowStock);
        Assert.Equal("Low Stock", product.Status);
    }

    [Fact]
    public void GetInventory_IdentifiesOutOfStockProduct()
    {
        var product = _context.Products.Single(p => p.Id == 1);

        product.StockQuantity = 0;

        _context.SaveChanges();

        var inventory = _repository.GetInventory("");

        var result = inventory.Single(p => p.ProductId == 1);

        Assert.True(result.IsOutOfStock);
        Assert.True(result.IsLowStock);
        Assert.Equal("Out of Stock", result.Status);
    }

    [Fact]
    public void GetNegativeStockProducts_ReturnsProductsWithNegativeStock()
    {
        var product = _context.Products.Single(p => p.Id == 1);

        product.StockQuantity = -3;

        _context.SaveChanges();

        var products = _repository.GetNegativeStockProducts();

        var result = Assert.Single(products);

        Assert.Equal(1, result.Id);
        Assert.Equal(-3, result.StockQuantity);
    }

    [Fact]
    public void GetInventory_FlagsNegativeStockBalance()
    {
        var product = _context.Products.Single(p => p.Id == 1);

        product.StockQuantity = -3;

        _context.SaveChanges();

        var inventory = _repository.GetInventory("");

        var result = inventory.Single(p => p.ProductId == 1);

        Assert.True(result.HasInvalidBalance);
        Assert.Equal("Invalid Balance", result.Status);
        Assert.Equal(-3, result.AvailableStock);
    }

    [Fact]
    public void GetNegativeStockProducts_ReturnsEmptyListWhenStockIsValid()
    {
        var products = _repository.GetNegativeStockProducts();

        Assert.Empty(products);
    }

    [Fact]
    public async Task ReceiveStock_IncreasesStockAndRecordsMovement()
    {
        await _repository.ReceiveStockAsync(
            1, 20, 1, "Supplier delivery");

        Assert.Equal(30, GetStockQuantity(1));
        Assert.Equal(5, GetStockQuantity(2));

        var movement = Assert.Single(
            _context.StockMovements.AsNoTracking().ToList());

        Assert.Equal(1, movement.ProductId);
        Assert.Equal(20, movement.QuantityChange);
        Assert.Equal(InventoryService.StockReceived, movement.MovementType);
        Assert.Equal(1, movement.UserId);
        Assert.Equal("Supplier delivery", movement.Notes);
        Assert.NotEqual(default(DateTime), movement.RecordedAt);
    }

    [Fact]
    public async Task ReceiveStock_RecordsMovementWithoutNotes()
    {
        await _repository.ReceiveStockAsync(1, 5, 1);

        Assert.Equal(15, GetStockQuantity(1));

        var movement = Assert.Single(
            _context.StockMovements.AsNoTracking().ToList());

        Assert.Equal(5, movement.QuantityChange);
        Assert.Null(movement.Notes);
    }

    [Theory]
    [InlineData(5, 15)]
    [InlineData(-4, 6)]
    [InlineData(-10, 0)]
    public async Task AdjustStock_UpdatesStockAndRecordsMovement(
        int quantityChange, int expectedStock)
    {
        await _repository.AdjustStockAsync(
            1, quantityChange, 1, "Stock count correction");

        Assert.Equal(expectedStock, GetStockQuantity(1));

        var movement = Assert.Single(
            _context.StockMovements.AsNoTracking().ToList());

        Assert.Equal(1, movement.ProductId);
        Assert.Equal(quantityChange, movement.QuantityChange);
        Assert.Equal("StockAdjustment", movement.MovementType);
        Assert.Equal(1, movement.UserId);
        Assert.Equal("Stock count correction", movement.Notes);
    }

    [Fact]
    public async Task AdjustStock_RejectsNegativeStock()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.AdjustStockAsync(
                1, -11, 1, "Stock count correction"));

        Assert.Equal(10, GetStockQuantity(1));
        Assert.Empty(_context.StockMovements.ToList());
    }

    [Fact]
    public async Task GetStockMovementHistory_ReturnsProductAndUserDetails()
    {
        await _repository.ReceiveStockAsync(
            1, 20, 1, "Supplier delivery");

        var movements = _repository.GetStockMovementHistory(1);

        var movement = Assert.Single(movements);

        Assert.Equal(1, movement.ProductId);
        Assert.Equal("Rice", movement.ProductName);
        Assert.Equal(20, movement.QuantityChange);
        Assert.Equal(InventoryService.StockReceived, movement.MovementType);
        Assert.Equal(1, movement.UserId);
        Assert.Equal("inventoryuser", movement.Username);
        Assert.Equal("Supplier delivery", movement.Notes);
        Assert.NotEqual(default(DateTime), movement.RecordedAt);
    }

    [Fact]
    public async Task GetStockMovementHistory_ReturnsOnlySelectedProduct()
    {
        await _repository.ReceiveStockAsync(1, 10, 1);
        await _repository.ReceiveStockAsync(2, 20, 1);

        var movements = _repository.GetStockMovementHistory(1);

        var movement = Assert.Single(movements);

        Assert.Equal(1, movement.ProductId);
        Assert.Equal("Rice", movement.ProductName);
    }

    [Fact]
    public async Task GetStockMovementHistory_ReturnsLatestMovementFirst()
    {
        await _repository.ReceiveStockAsync(
            1, 10, 1, "Supplier delivery");

        await _repository.AdjustStockAsync(
            1, -3, 1, "Damaged items");

        var movements = _repository.GetStockMovementHistory(1);

        Assert.Equal(2, movements.Count);

        Assert.Equal("StockAdjustment", movements[0].MovementType);
        Assert.Equal(-3, movements[0].QuantityChange);

        Assert.Equal(InventoryService.StockReceived, movements[1].MovementType);
        Assert.Equal(10, movements[1].QuantityChange);
    }

    [Fact]
    public void GetStockMovementHistory_ReturnsEmptyListWhenNoMovements()
    {
        var movements = _repository.GetStockMovementHistory(1);

        Assert.Empty(movements);
    }

    [Fact]
    public async Task ReceiveStock_RollsBackStockChangeWithInvalidUser()
    {
        await Assert.ThrowsAsync<DbUpdateException>(
            () => _repository.ReceiveStockAsync(
                1, 20, 999, "Supplier delivery"));

        _context.ChangeTracker.Clear();

        Assert.Equal(10, GetStockQuantity(1));
        Assert.Empty(_context.StockMovements.ToList());
    }
}