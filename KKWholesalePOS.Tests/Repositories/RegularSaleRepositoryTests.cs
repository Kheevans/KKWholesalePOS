
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Sales;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Tests.Repositories;

public class RegularSaleRepositoryTests
{
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

    private static Product CreateProduct(int stock = 10)
    {
        return new Product
        {
            Name = "Test Product",
            Barcode = "TEST001",
            SellingPrice = 10m,
            StockQuantity = stock,
            UnitsPerCase = 1,
            MinimumStockLevel = 2
        };
    }

    [Fact]
    public void SaveCashSale_DeductsStockAndRecordsMovement()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = CreateUser(context);
            var product = CreateProduct();

            context.Products.Add(product);
            context.SaveChanges();

            var repository = new RegularSaleRepository(context);

            var sale = repository.SaveCashSale(
                new List<SaleRequestLine>
                {
                    new(product.Id, 2)
                },
                25m,
                user.Id,
                user.Username);

            var updatedProduct = context.Products
                .AsNoTracking()
                .Single(p => p.Id == product.Id);

            var movement = context.StockMovements
                .Single();

            Assert.Equal(20m, sale.Total);
            Assert.Equal(25m, sale.CashReceived);
            Assert.Equal(5m, sale.ChangeDue);
            Assert.Equal("Cash", sale.PaymentMethod);

            Assert.Equal(8, updatedProduct.StockQuantity);

            Assert.Equal(-2, movement.QuantityChange);
            Assert.Equal(
                "RegularSale",
                movement.MovementType);
            Assert.Equal(sale.Id, movement.RegularSaleId);
            Assert.Equal(user.Id, movement.UserId);
        }
    }

    [Fact]
    public void SaveCardSale_DeductsStockAndRecordsCardDetails()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = CreateUser(context);
            var product = CreateProduct();

            context.Products.Add(product);
            context.SaveChanges();

            var repository = new RegularSaleRepository(context);

            var sale = repository.SaveCardSale(
                new List<SaleRequestLine>
                {
                    new(product.Id, 3)
                },
                "Visa",
                "1234",
                "APPROVAL001",
                user.Id,
                user.Username);

            var updatedProduct = context.Products
                .AsNoTracking()
                .Single(p => p.Id == product.Id);

            var movement = context.StockMovements
                .Single();

            Assert.Equal(30m, sale.Total);
            Assert.Equal(30m, sale.CashReceived);
            Assert.Equal(0m, sale.ChangeDue);
            Assert.Equal("Card", sale.PaymentMethod);

            Assert.Equal("Visa", sale.CardType);
            Assert.Equal("1234", sale.CardLastFour);
            Assert.Equal("APPROVAL001", sale.ApprovalCode);

            Assert.Equal(7, updatedProduct.StockQuantity);
            Assert.Equal(-3, movement.QuantityChange);
            Assert.Equal(sale.Id, movement.RegularSaleId);
        }
    }

    [Fact]
    public void SaveCashSale_CombinesDuplicateProductLines()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = CreateUser(context);
            var product = CreateProduct();

            context.Products.Add(product);
            context.SaveChanges();

            var repository = new RegularSaleRepository(context);

            var sale = repository.SaveCashSale(
                new List<SaleRequestLine>
                {
                    new(product.Id, 1),
                    new(product.Id, 2)
                },
                30m,
                user.Id,
                user.Username);

            var updatedProduct = context.Products
                .AsNoTracking()
                .Single(p => p.Id == product.Id);

            var movements = context.StockMovements.ToList();

            Assert.Single(sale.Lines);
            Assert.Equal(3, sale.Lines[0].Quantity);
            Assert.Equal(30m, sale.Total);

            Assert.Equal(7, updatedProduct.StockQuantity);

            Assert.Single(movements);
            Assert.Equal(-3, movements[0].QuantityChange);
        }
    }

    [Fact]
    public void SaveCashSale_InsufficientStockRollsBackSaleAndMovement()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = CreateUser(context);
            var product = CreateProduct(stock: 2);

            context.Products.Add(product);
            context.SaveChanges();

            var repository = new RegularSaleRepository(context);

            Assert.Throws<InvalidOperationException>(() =>
                repository.SaveCashSale(
                    new List<SaleRequestLine>
                    {
                        new(product.Id, 3)
                    },
                    30m,
                    user.Id,
                    user.Username));

            var updatedProduct = context.Products
                .AsNoTracking()
                .Single(p => p.Id == product.Id);

            Assert.Equal(2, updatedProduct.StockQuantity);
            Assert.Empty(context.RegularSales.ToList());
            Assert.Empty(context.StockMovements.ToList());
        }
    }

    [Fact]
    public void SaveCashSale_InvalidPaymentDoesNotSaveSaleOrDeductStock()
    {
        var (connection, context) = CreateContext();

        using (connection)
        using (context)
        {
            var user = CreateUser(context);
            var product = CreateProduct();

            context.Products.Add(product);
            context.SaveChanges();

            var repository = new RegularSaleRepository(context);

            Assert.Throws<ArgumentException>(() =>
                repository.SaveCashSale(
                    new List<SaleRequestLine>
                    {
                        new(product.Id, 2)
                    },
                    15m,
                    user.Id,
                    user.Username));

            var updatedProduct = context.Products
                .AsNoTracking()
                .Single(p => p.Id == product.Id);

            Assert.Equal(10, updatedProduct.StockQuantity);
            Assert.Empty(context.RegularSales.ToList());
            Assert.Empty(context.StockMovements.ToList());
        }
    }
}
