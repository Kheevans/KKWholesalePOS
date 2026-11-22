
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Sales;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Tests.Repositories;

public class DashboardRepositoryTests
{
    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    private static DateTime ToUtc(DateTime localDate)
    {
        return DateTime.SpecifyKind(
            localDate, DateTimeKind.Local).ToUniversalTime();
    }

    [Fact]
    public void GetDashboardSummary_CountsProductsCustomersAndStockLevels()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext(connection);

        context.Products.AddRange(
            new Product
            {
                Name = "Rice",
                Barcode = "111111",
                SellingPrice = 500m,
                StockQuantity = 0,
                MinimumStockLevel = 5
            },
            new Product
            {
                Name = "Sugar",
                Barcode = "222222",
                SellingPrice = 300m,
                StockQuantity = 3,
                MinimumStockLevel = 5
            },
            new Product
            {
                Name = "Flour",
                Barcode = "333333",
                SellingPrice = 400m,
                StockQuantity = 15,
                MinimumStockLevel = 5
            });

        context.Customers.AddRange(
            new Customer { Name = "Customer One" },
            new Customer { Name = "Customer Two" });

        context.SaveChanges();

        var repository = new DashboardRepository(context);

        var result = repository.GetDashboardSummary();

        Assert.Equal(3, result.TotalProducts);
        Assert.Equal(2, result.TotalCustomers);
        Assert.Equal(1, result.LowStockProducts);
        Assert.Equal(1, result.OutOfStockProducts);
    }

    [Fact]
    public void GetDashboardSummary_CalculatesTodaysSalesCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext(connection);

        var user = new User
        {
            Username = "salestest",
            PasswordHash = "test-hash"
        };

        context.Users.Add(user);
        context.SaveChanges();

        DateTime today = DateTime.Today;

        context.RegularSales.AddRange(
            new RegularSale
            {
                CompletedAt = ToUtc(today.AddHours(9)),
                CashierId = user.Id,
                CashierName = user.Username,
                Total = 1500m,
                CashReceived = 1500m,
                ChangeDue = 0m,
                PaymentMethod = "Cash"
            },
            new RegularSale
            {
                CompletedAt = ToUtc(today.AddHours(14)),
                CashierId = user.Id,
                CashierName = user.Username,
                Total = 2500m,
                CashReceived = 2500m,
                ChangeDue = 0m,
                PaymentMethod = "Cash"
            },
            new RegularSale
            {
                CompletedAt = ToUtc(today.AddDays(-1).AddHours(12)),
                CashierId = user.Id,
                CashierName = user.Username,
                Total = 5000m,
                CashReceived = 5000m,
                ChangeDue = 0m,
                PaymentMethod = "Cash"
            });

        context.SaveChanges();

        var repository = new DashboardRepository(context);

        var result = repository.GetDashboardSummary();

        Assert.Equal(4000m, result.SalesToday);
        Assert.Equal(2, result.TransactionsToday);
    }

    [Fact]
    public void GetDashboardSummary_CalculatesOutstandingCreditAndOverdueAccounts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Credit Customer" };

        var user = new User
        {
            Username = "credittest",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var overdueSale = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(DateTime.Today.AddDays(-20).AddHours(10)),
            DueDate = ToUtc(DateTime.Today.AddDays(-6)),
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-DASH-001"
        };

        var currentSale = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(DateTime.Today.AddDays(-5).AddHours(10)),
            DueDate = ToUtc(DateTime.Today.AddDays(9)),
            PurchaseAmount = 5000m,
            CreditFee = 250m,
            ReferenceNumber = "CR-DASH-002"
        };

        context.CreditSales.AddRange(overdueSale, currentSale);
        context.SaveChanges();

        context.CreditPayments.Add(new CreditPayment
        {
            CreditSaleId = overdueSale.Id,
            UserId = user.Id,
            PaymentDate = ToUtc(DateTime.Today.AddDays(-2)),
            Amount = 2000m,
            PaymentMethod = "Cash"
        });

        context.SaveChanges();

        var repository = new DashboardRepository(context);

        var result = repository.GetDashboardSummary();

        // (10,000 + 500 - 2,000) + (5,000 + 250)
        Assert.Equal(13750m, result.OutstandingCreditBalance);

        // Only the first account is past its 14-day due period.
        Assert.Equal(1, result.OverdueCreditAccounts);
    }

    [Fact]
    public void GetDashboardSummary_CalculatesOutstandingConsignmentBalance()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Consignment Customer" };

        var user = new User
        {
            Username = "consignmenttest",
            PasswordHash = "test-hash"
        };

        var product = new Product
        {
            Name = "Rice",
            Barcode = "444444",
            SellingPrice = 500m,
            StockQuantity = 20,
            MinimumStockLevel = 5
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.Add(product);
        context.SaveChanges();

        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(DateTime.Today.AddDays(-5)),
            ReferenceNumber = "CON-DASH-001",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        var consignmentLine = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Barcode = product.Barcode,
            UnitPrice = 500m,
            Quantity = 20,
            LineTotal = 10000m
        };

        context.ConsignmentLines.Add(consignmentLine);
        context.SaveChanges();

        var returnHeader = new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            ReturnDate = ToUtc(DateTime.Today.AddDays(-2)),
            ReferenceNumber = "RET-DASH-001"
        };

        context.ConsignmentReturns.Add(returnHeader);
        context.SaveChanges();

        context.ConsignmentReturnLines.Add(new ConsignmentReturnLine
        {
            ConsignmentReturnId = returnHeader.Id,
            ConsignmentLineId = consignmentLine.Id,
            QuantityReturned = 5,
            QuantityAccepted = 4,
            UnitPrice = 500m
        });

        context.ConsignmentPayments.Add(new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            PaymentDate = ToUtc(DateTime.Today.AddDays(-1)),
            Amount = 3000m,
            PaymentMethod = "Cash"
        });

        context.SaveChanges();

        var repository = new DashboardRepository(context);

        var result = repository.GetDashboardSummary();

        // 10,000 original value - 2,000 accepted returns
        // + 500 fee - 3,000 payments = 5,500.
        Assert.Equal(5500m, result.OutstandingConsignmentBalance);
        Assert.Equal(1, result.PendingConsignments);
    }

    [Fact]
    public void GetDashboardSummary_ReturnsZeroBalancesWhenNoTransactionsExist()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext(connection);

        var repository = new DashboardRepository(context);

        var result = repository.GetDashboardSummary();

        Assert.Equal(0m, result.SalesToday);
        Assert.Equal(0, result.TransactionsToday);
        Assert.Equal(0m, result.OutstandingCreditBalance);
        Assert.Equal(0, result.OverdueCreditAccounts);
        Assert.Equal(0m, result.OutstandingConsignmentBalance);
        Assert.Equal(0, result.PendingConsignments);
    }
}
