
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Tests.Services;

public class InventoryServiceTests
{
    private static User CreateUser(AppDbContext context)
    {
        var user = new User
        {
            Username = "testcashier",
            PasswordHash = "test-hash"
        };

        context.Users.Add(user);
        context.SaveChanges();

        return user;
    }

    private static (SqliteConnection Connection, AppDbContext Context)
        CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    private static Product CreateProduct()
    {
        return new Product
        {
            Name = "Test Product",
            Barcode = "TEST001",
            SellingPrice = 100m,
            StockQuantity = 10,
            UnitsPerCase = 1,
            MinimumStockLevel = 5
        };
    }

    [Fact]
    public async Task ReceiveStock_IncreasesStockAndRecordsMovement()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var product = CreateProduct();
            context.Products.Add(product);

            var user = CreateUser(context);

            context.SaveChanges();

            var service = new InventoryService(context);

            await service.ReceiveStockAsync(
                product.Id, 15, user.Id, "Supplier delivery");

            await context.Entry(product).ReloadAsync();

            var movement = await context.StockMovements
                .SingleAsync();

            Assert.Equal(25, product.StockQuantity);
            Assert.Equal(15, movement.QuantityChange);
            Assert.Equal(InventoryService.StockReceived, movement.MovementType);
            Assert.Equal("Supplier delivery", movement.Notes);
            Assert.Equal(1, movement.UserId);
        }
    }

    [Fact]
    public async Task ReceiveStock_RejectsZeroOrNegativeQuantity()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ReceiveStockAsync(1, 0, 1));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ReceiveStockAsync(1, -5, 1));
        }
    }

    [Fact]
    public async Task AdjustStock_RecordsPositiveAdjustment()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var product = CreateProduct();
            context.Products.Add(product);

            var user = CreateUser(context);

            context.SaveChanges();

            var service = new InventoryService(context);

            await service.AdjustStockAsync(
                product.Id, 5, user.Id, "Physical stock count");

            await context.Entry(product).ReloadAsync();

            Assert.Equal(15, product.StockQuantity);

            var movement = await context.StockMovements.SingleAsync();

            Assert.Equal(5, movement.QuantityChange);
            Assert.Equal(
                InventoryService.StockAdjustment,
                movement.MovementType);
        }
    }

    [Fact]
    public async Task AdjustStock_RecordsNegativeAdjustment()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var product = CreateProduct();
            context.Products.Add(product);

            var user = CreateUser(context);

            context.SaveChanges();

            var service = new InventoryService(context);

            await service.AdjustStockAsync(
                product.Id, -3, user.Id, "Damaged goods");

            await context.Entry(product).ReloadAsync();

            Assert.Equal(7, product.StockQuantity);

            var movement = await context.StockMovements.SingleAsync();

            Assert.Equal(-3, movement.QuantityChange);
            Assert.Equal(
                InventoryService.StockAdjustment,
                movement.MovementType);
        }
    }

    [Fact]
    public async Task AdjustStock_RejectsAdjustmentThatCausesNegativeStock()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var product = CreateProduct();
            context.Products.Add(product);
            context.SaveChanges();

            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdjustStockAsync(
                    product.Id, -15, 1, "Incorrect stock count"));

            Assert.Equal(
                10,
                (await context.Products.FindAsync(product.Id))!.StockQuantity);

            Assert.Empty(context.StockMovements);
        }
    }

    [Fact]
    public async Task AdjustStock_RequiresReason()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AdjustStockAsync(1, 5, 1, ""));
        }
    }

    [Fact]
    public async Task AdjustStock_RejectsZeroQuantityChange()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AdjustStockAsync(1, 0, 1, "Stock count"));
        }
    }

    [Fact]
    public async Task ReceiveStock_RejectsNonexistentProduct()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReceiveStockAsync(999, 10, 1));
        }
    }


    [Fact]
    public void ApplyStockChange_RequiresActiveTransaction()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var service = new InventoryService(context);

            Assert.Throws<InvalidOperationException>(() =>
                service.ApplyStockChange(
                    productId: 1,
                    quantityChange: -2,
                    userId: 1,
                    movementType: InventoryService.RegularSale,
                    regularSaleId: 1));
        }
    }

    [Fact]
    public async Task ApplyStockChange_RejectsMismatchedMovementDetails()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = new User
            {
                Username = "movementtester",
                PasswordHash = "test-hash"
            };

            var product = CreateProduct();

            context.Users.Add(user);
            context.Products.Add(product);
            context.SaveChanges();

            var service = new InventoryService(context);

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            Assert.Throws<ArgumentException>(() =>
                service.ApplyStockChange(
                    productId: product.Id,
                    quantityChange: -2,
                    userId: user.Id,
                    movementType: InventoryService.RegularSale));

            await transaction.RollbackAsync();

            var updatedProduct = await context.Products
                .AsNoTracking()
                .SingleAsync(p => p.Id == product.Id);

            Assert.Equal(10, updatedProduct.StockQuantity);

            Assert.Empty(await context.StockMovements.ToListAsync());
        }
    }

}
