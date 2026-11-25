
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using KKWholesalePOS.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KKWholesalePOS.Tests.Repositories;

public class ConsignmentReturnRepositoryTests
{
    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
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

    private static Customer CreateCustomer(AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Test Customer",
            PhoneNumber = "1234567890",
            Address = "Test Address"
        };

        context.Customers.Add(customer);
        context.SaveChanges();

        return customer;
    }

    private static Product CreateProduct(AppDbContext context)
    {
        var product = new Product
        {
            Name = "Test Product",
            Barcode = "123456789",
            SellingPrice = 20m,
            StockQuantity = 10,
            UnitsPerCase = 1,
            MinimumStockLevel = 5
        };

        context.Products.Add(product);
        context.SaveChanges();

        return product;
    }

    private static Consignment CreateConsignment(
        AppDbContext context,
        int quantity = 5,
        decimal unitPrice = 20m)
    {
        var user = CreateUser(context);
        var customer = CreateCustomer(context);
        var product = CreateProduct(context);

        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            ReferenceNumber = "CON-TEST-001",
            OriginalGoodsValue = quantity * unitPrice,
            ConsignmentFee = quantity * unitPrice * 0.05m,
            Lines = new List<ConsignmentLine>
            {
                new ConsignmentLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    LineTotal = quantity * unitPrice
                }
            }
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        return consignment;
    }

    private static ConsignmentReturnRepository CreateRepository(
        AppDbContext context)
    {
        var inventoryService = new InventoryService(context);
        return new ConsignmentReturnRepository(context);
    }

    [Fact]
    public void FindConsignmentByReference_ReturnsConsignment_WhenReferenceExists()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var repository = CreateRepository(context);

        var result = repository.FindConsignmentByReference(
            consignment.ReferenceNumber);

        Assert.NotNull(result);
        Assert.Equal(consignment.ReferenceNumber, result.ReferenceNumber);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void FindConsignmentByReference_ReturnsNull_WhenReferenceDoesNotExist()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var repository = CreateRepository(context);

        var result = repository.FindConsignmentByReference("INVALID-REF");

        Assert.Null(result);
    }

    [Fact]
    public void GetConsignmentsByCustomerId_ReturnsOnlyCustomerConsignments()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var firstConsignment = CreateConsignment(context);

        var secondCustomer = CreateCustomer(context);
        var user = context.Users.First();

        context.Consignments.Add(new Consignment
        {
            CustomerId = secondCustomer.Id,
            UserId = user.Id,
            ReferenceNumber = "CON-TEST-002",
            OriginalGoodsValue = 100m,
            ConsignmentFee = 5m
        });

        context.SaveChanges();

        var repository = CreateRepository(context);

        var result = repository.GetConsignmentsByCustomerId(
            firstConsignment.CustomerId);

        Assert.Single(result);
        Assert.Equal(firstConsignment.Id, result[0].Id);
    }

    [Fact]
    public void SaveReturn_AddsReturnAndRestoresAcceptedQuantityToInventory()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var consignmentLine = consignment.Lines.First();
        var product = context.Products.First();

        var repository = CreateRepository(context);

        var consignmentReturn = new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            ReferenceNumber = "RET-TEST-001",
            Lines = new List<ConsignmentReturnLine>
            {
                new ConsignmentReturnLine
                {
                    ConsignmentLineId = consignmentLine.Id,
                    QuantityReturned = 2,
                    QuantityAccepted = 2,
                    UnitPrice = consignmentLine.UnitPrice
                }
            }
        };

        repository.SaveReturn(consignmentReturn);

        context.Entry(product).Reload();

        Assert.Equal(12, product.StockQuantity);
        Assert.Single(context.ConsignmentReturns);
        Assert.Single(context.ConsignmentReturnLines);

        var stockMovement = Assert.Single(context.StockMovements);
        Assert.Equal(2, stockMovement.QuantityChange);
        Assert.Equal(InventoryService.ConsignmentReturn, stockMovement.MovementType);
        Assert.Equal(consignmentReturn.Id, stockMovement.ConsignmentReturnId);
    }

    [Fact]
    public void SaveReturn_DoesNotRestoreInventory_WhenNoItemsAreAccepted()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var consignmentLine = consignment.Lines.First();
        var product = context.Products.First();

        var repository = CreateRepository(context);

        var consignmentReturn = new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            ReferenceNumber = "RET-TEST-002",
            Lines = new List<ConsignmentReturnLine>
            {
                new ConsignmentReturnLine
                {
                    ConsignmentLineId = consignmentLine.Id,
                    QuantityReturned = 2,
                    QuantityAccepted = 0,
                    UnitPrice = consignmentLine.UnitPrice
                }
            }
        };

        repository.SaveReturn(consignmentReturn);

        context.Entry(product).Reload();

        Assert.Equal(10, product.StockQuantity);
        Assert.Empty(context.StockMovements);
        Assert.Single(context.ConsignmentReturns);
    }

    [Fact]
    public void GetReturns_ReturnsReturnsForSpecifiedConsignment()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var consignmentLine = consignment.Lines.First();
        var repository = CreateRepository(context);

        repository.SaveReturn(new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            ReferenceNumber = "RET-TEST-003",
            Lines = new List<ConsignmentReturnLine>
            {
                new ConsignmentReturnLine
                {
                    ConsignmentLineId = consignmentLine.Id,
                    QuantityReturned = 1,
                    QuantityAccepted = 1,
                    UnitPrice = consignmentLine.UnitPrice
                }
            }
        });

        var result = repository.GetReturns(consignment.Id);

        Assert.Single(result);
        Assert.Equal("RET-TEST-003", result[0].ReferenceNumber);
        Assert.Single(result[0].Lines);
    }

    [Fact]
    public void SavePayment_AddsPayment_WhenAmountIsWithinOutstandingBalance()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var repository = CreateRepository(context);

        var payment = new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            Amount = 50m,
            PaymentMethod = "Cash"
        };

        repository.SavePayment(payment);

        var savedPayment = Assert.Single(context.ConsignmentPayments);

        Assert.Equal(50m, savedPayment.Amount);
        Assert.Equal(consignment.Id, savedPayment.ConsignmentId);
        Assert.Equal("Cash", savedPayment.PaymentMethod);
    }

    [Fact]
    public void SavePayment_ThrowsException_WhenPaymentExceedsOutstandingBalance()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var repository = CreateRepository(context);

        // Original goods value = $100
        // 5% fee = $5
        // Outstanding balance = $105

        var payment = new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            Amount = 106m,
            PaymentMethod = "Cash"
        };

        Assert.Throws<InvalidOperationException>(
            () => repository.SavePayment(payment));

        Assert.Empty(context.ConsignmentPayments);
    }

    [Fact]
    public void GetPayments_ReturnsPaymentsForSpecifiedConsignment()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var consignment = CreateConsignment(context);
        var repository = CreateRepository(context);

        repository.SavePayment(new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = consignment.UserId,
            Amount = 25m,
            PaymentMethod = "Cash"
        });

        var result = repository.GetPayments(consignment.Id);

        Assert.Single(result);
        Assert.Equal(25m, result[0].Amount);
    }
}
