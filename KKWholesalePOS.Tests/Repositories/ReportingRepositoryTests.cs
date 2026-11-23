using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;

namespace KKWholesalePOS.Tests.Repositories;

public class ReportingRepositoryTests
{
    [Fact]
    public void GetCreditStatement_CalculatesOutstandingBalanceCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer
        {
            Name = "Test Customer"
        };

        var user = new User
        {
            Username = "testcashier",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        DateTime ToUtc(int day, int month, int year) =>
            new DateTime(year, month, day, 10, 0, 0, DateTimeKind.Local)
                .ToUniversalTime();

        var sale = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(3, 11, 2026),
            DueDate = ToUtc(17, 11, 2026),
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-TEST-001"
        };

        context.CreditSales.Add(sale);
        context.SaveChanges();

        context.CreditPayments.AddRange(
            new CreditPayment
            {
                CreditSaleId = sale.Id,
                UserId = user.Id,
                PaymentDate = ToUtc(10, 11, 2026),
                Amount = 2000m
            },
            new CreditPayment
            {
                CreditSaleId = sale.Id,
                UserId = user.Id,
                PaymentDate = ToUtc(30, 11, 2026),
                Amount = 1500m
            },
            new CreditPayment
            {
                CreditSaleId = sale.Id,
                UserId = user.Id,
                PaymentDate = ToUtc(1, 12, 2026),
                Amount = 3000m
            });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetCreditStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        Assert.Single(result.Transactions);
        Assert.Equal("Test Customer", result.CustomerName);

        Assert.Equal(10000m, result.TotalPurchases);
        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(3500m, result.TotalPayments);
        Assert.Equal(7000m, result.TotalOutstanding);

        Assert.Equal(7000m, result.Transactions[0].OutstandingBalance);
    }

    [Fact]
    public void GetCreditStatement_ThrowsWhenCustomerDoesNotExist()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var repository = new ReportingRepository(context);

        Assert.Throws<InvalidOperationException>(() =>
            repository.GetCreditStatement(
                999,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 30)));
    }


    [Fact]
    public void GetCreditStatement_IncludesOutstandingPurchaseFromPreviousPeriod()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "testcashier",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var sale = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 11, 17),
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-TEST-003"
        };

        context.CreditSales.Add(sale);
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetCreditStatement(
            customer.Id,
            new DateTime(2026, 11, 10),
            new DateTime(2026, 11, 30));

        Assert.Single(result.Transactions);
        Assert.Equal(10000m, result.TotalPurchases);
        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(10500m, result.TotalOutstanding);
        Assert.Equal(
            new DateTime(2026, 11, 17),
            result.Transactions[0].DueDate);
    }


    [Fact]
    public void GetCreditStatement_ExcludesPurchasePaidBeforeReportingPeriod()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "testcashier",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var sale = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 11, 17),
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-TEST-004"
        };

        context.CreditSales.Add(sale);
        context.SaveChanges();

        context.CreditPayments.Add(new CreditPayment
        {
            CreditSaleId = sale.Id,
            UserId = user.Id,
            PaymentDate = new DateTime(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc),
            Amount = 10500m
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetCreditStatement(
            customer.Id,
            new DateTime(2026, 11, 10),
            new DateTime(2026, 11, 30));

        Assert.Empty(result.Transactions);
        Assert.Equal(0m, result.TotalOutstanding);
    }

    [Fact]
    public void GetInventoryReport_ReturnsCorrectStockAndConsignmentQuantities()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "inventorytest",
            PasswordHash = "test-hash"
        };

        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456",
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
            ReferenceNumber = "CON-TEST-001",
            OriginalGoodsValue = 5000m,
            ConsignmentFee = 250m
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        var consignmentLine = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Barcode = product.Barcode,
            UnitPrice = product.SellingPrice,
            Quantity = 10,
            LineTotal = 5000m
        };

        context.ConsignmentLines.Add(consignmentLine);
        context.SaveChanges();

        var consignmentReturn = new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            ReferenceNumber = "RET-TEST-001"
        };

        context.ConsignmentReturns.Add(consignmentReturn);
        context.SaveChanges();

        context.ConsignmentReturnLines.Add(new ConsignmentReturnLine
        {
            ConsignmentReturnId = consignmentReturn.Id,
            ConsignmentLineId = consignmentLine.Id,
            QuantityReturned = 4,
            QuantityAccepted = 3,
            UnitPrice = 500m
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetInventoryReport();

        var row = Assert.Single(result.Products);

        Assert.Equal(product.Id, row.ProductId);
        Assert.Equal("Rice", row.ProductName);
        Assert.Equal(20, row.AvailableStock);
        Assert.Equal(7, row.OutstandingConsignmentQuantity);
        Assert.Equal(5, row.MinimumStockLevel);
        Assert.False(row.IsLowStock);
    }

    [Fact]
    public void GetInventoryReport_IdentifiesLowStockProducts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        context.Products.AddRange(
            new Product
            {
                Name = "Sugar",
                Barcode = "111111",
                SellingPrice = 300m,
                StockQuantity = 3,
                MinimumStockLevel = 5
            },
            new Product
            {
                Name = "Flour",
                Barcode = "222222",
                SellingPrice = 400m,
                StockQuantity = 15,
                MinimumStockLevel = 5
            });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetInventoryReport();

        Assert.Equal(2, result.TotalProducts);
        Assert.Equal(1, result.LowStockProductCount);

        var lowStockProduct = result.Products
            .Single(product => product.ProductName == "Sugar");

        Assert.True(lowStockProduct.IsLowStock);
    }

    [Fact]
    public void GetInventoryReport_ReturnsZeroOutstandingConsignmentQuantity()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var product = new Product
        {
            Name = "Cooking Oil",
            Barcode = "333333",
            SellingPrice = 800m,
            StockQuantity = 25,
            MinimumStockLevel = 5
        };

        context.Products.Add(product);
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetInventoryReport();

        var row = Assert.Single(result.Products);

        Assert.Equal(25, row.AvailableStock);
        Assert.Equal(0, row.OutstandingConsignmentQuantity);
        Assert.Equal(0, result.LowStockProductCount);
    }

    [Fact]
    public void GetConsignmentStatement_CalculatesOutstandingBalanceCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "consignmenttest",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();


        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-001",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456789",
            SellingPrice = 500,
            StockQuantity = 20,
            UnitsPerCase = 1,
            MinimumStockLevel = 5
        };

        context.Products.Add(product);
        context.SaveChanges();

        var consignmentLine = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = "Rice",
            Barcode = "123456",
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
            ReturnDate = new DateTime(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "RET-TEST-001"
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
            PaymentDate = new DateTime(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc),
            Amount = 2000m,
            PaymentMethod = "Cash"
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Transactions);

        Assert.Equal(10000m, row.OriginalGoodsValue);
        Assert.Equal(2000m, row.AcceptedReturnsValue);
        Assert.Equal(500m, row.ConsignmentFee);
        Assert.Equal(2000m, row.Payments);
        Assert.Equal(6500m, row.FinalBalance);

        Assert.Equal(6500m, result.TotalOutstanding);
    }

    [Fact]
    public void GetConsignmentStatement_IncludesEarlierOutstandingConsignment()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "consignmenttest",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        context.Consignments.Add(new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-002",
            OriginalGoodsValue = 5000m,
            ConsignmentFee = 250m
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Transactions);

        Assert.Equal("CON-TEST-002", row.ReferenceNumber);
        Assert.Equal(5250m, row.FinalBalance);
    }

    [Fact]
    public void GetConsignmentStatement_ExcludesFullySettledEarlierConsignment()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "consignmenttest",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-003",
            OriginalGoodsValue = 5000m,
            ConsignmentFee = 250m
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        context.ConsignmentPayments.Add(new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            PaymentDate = new DateTime(2026, 10, 25, 15, 0, 0, DateTimeKind.Utc),
            Amount = 5250m,
            PaymentMethod = "Cash"
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        Assert.Empty(result.Transactions);
        Assert.Equal(0m, result.TotalOutstanding);
    }

    [Fact]
    public void GetConsignmentStatement_ThrowsWhenCustomerDoesNotExist()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var repository = new ReportingRepository(context);

        Assert.Throws<InvalidOperationException>(() =>
            repository.GetConsignmentStatement(
                999,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 30)));
    }

    [Fact]
    public void GetConsignmentStatement_ExcludesReturnsAndPaymentsAfterPeriod()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var customer = new Customer { Name = "Test Customer" };
        var user = new User
        {
            Username = "consignmenttest",
            PasswordHash = "test-hash"
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-004",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456789",
            SellingPrice = 500,
            StockQuantity = 20,
            UnitsPerCase = 1,
            MinimumStockLevel = 5
        };

        context.Products.Add(product);
        context.SaveChanges();

        var consignmentLine = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = "Rice",
            Barcode = "123456",
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
            ReturnDate = new DateTime(2026, 12, 2, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "RET-TEST-004"
        };

        context.ConsignmentReturns.Add(returnHeader);
        context.SaveChanges();

        context.ConsignmentReturnLines.Add(new ConsignmentReturnLine
        {
            ConsignmentReturnId = returnHeader.Id,
            ConsignmentLineId = consignmentLine.Id,
            QuantityReturned = 5,
            QuantityAccepted = 5,
            UnitPrice = 500m
        });

        context.ConsignmentPayments.Add(new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            PaymentDate = new DateTime(2026, 12, 5, 15, 0, 0, DateTimeKind.Utc),
            Amount = 2000m,
            PaymentMethod = "Cash"
        });

        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Transactions);

        Assert.Equal(0m, row.AcceptedReturnsValue);
        Assert.Equal(0m, row.Payments);
        Assert.Equal(10500m, row.FinalBalance);
    }
}