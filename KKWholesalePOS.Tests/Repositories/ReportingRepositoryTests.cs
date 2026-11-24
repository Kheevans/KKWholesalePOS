using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Consignment;

namespace KKWholesalePOS.Tests.Repositories;

public class ReportingRepositoryTests
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

    private static DateTime ToUtc(int day, int month, int year) =>
        new DateTime(year, month, day, 10, 0, 0, DateTimeKind.Local)
            .ToUniversalTime();

    #region Credit Report (Summary)

    [Fact]
    public void GetCreditReport_SummarizesCreditAccountsInDateRange()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Brown Wholesale" };
        var user = new User { Username = "testcashier", PasswordHash = "test-hash" };
        context.Customers.Add(customer);
        context.Users.Add(user);
        context.SaveChanges();

        var saleInRange = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(5, 11, 2026),
            DueDate = ToUtc(19, 11, 2026),
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-001"
        };

        var saleOutOfRange = new CreditSale
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = ToUtc(25, 10, 2026),
            DueDate = ToUtc(8, 11, 2026),
            PurchaseAmount = 4000m,
            CreditFee = 200m,
            ReferenceNumber = "CR-000"
        };

        context.CreditSales.AddRange(saleInRange, saleOutOfRange);
        context.SaveChanges();

        context.CreditPayments.Add(new CreditPayment
        {
            CreditSaleId = saleInRange.Id,
            UserId = user.Id,
            PaymentDate = ToUtc(12, 11, 2026),
            Amount = 3500m
        });
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetCreditReport(
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Accounts);
        Assert.Equal("CR-001", row.ReferenceNumber);
        Assert.Equal("Brown Wholesale", row.CustomerName);
        Assert.Equal(10000m, row.PurchaseAmount);
        Assert.Equal(500m, row.CreditFee);
        Assert.Equal(3500m, row.AmountPaid);
        Assert.Equal(7000m, row.OutstandingBalance);

        Assert.Equal(10000m, result.TotalPurchases);
        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(3500m, result.TotalPaid);
        Assert.Equal(7000m, result.TotalOutstanding);
    }

    #endregion

    #region Consignment Report (Summary)

    [Fact]
    public void GetConsignmentReport_SummarizesConsignmentsInDateRange()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Apex Traders" };
        var user = new User { Username = "consignmenttest", PasswordHash = "test-hash" };
        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456",
            SellingPrice = 500m,
            StockQuantity = 50,
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
            IssuedAt = ToUtc(4, 11, 2026),
            ReferenceNumber = "CON-001",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };
        context.Consignments.Add(consignment);
        context.SaveChanges();

        var line = new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = "Rice",
            Barcode = "123456",
            UnitPrice = 500m,
            Quantity = 20,
            LineTotal = 10000m
        };
        context.ConsignmentLines.Add(line);
        context.SaveChanges();

        var returnHeader = new ConsignmentReturn
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            ReturnDate = ToUtc(10, 11, 2026),
            ReferenceNumber = "RET-001"
        };
        context.ConsignmentReturns.Add(returnHeader);
        context.SaveChanges();

        context.ConsignmentReturnLines.Add(new ConsignmentReturnLine
        {
            ConsignmentReturnId = returnHeader.Id,
            ConsignmentLineId = line.Id,
            QuantityReturned = 5,
            QuantityAccepted = 4,
            UnitPrice = 500m
        });

        context.ConsignmentPayments.Add(new ConsignmentPayment
        {
            ConsignmentId = consignment.Id,
            UserId = user.Id,
            PaymentDate = ToUtc(15, 11, 2026),
            Amount = 2000m,
            PaymentMethod = "Cash"
        });
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentReport(
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Consignments);
        Assert.Equal("CON-001", row.ReferenceNumber);
        Assert.Equal("Apex Traders", row.CustomerName);
        Assert.Equal(10000m, row.OriginalGoodsValue);
        Assert.Equal(2000m, row.AcceptedReturnsValue);
        Assert.Equal(500m, row.ConsignmentFee);
        Assert.Equal(2000m, row.Payments);
        Assert.Equal(6500m, row.OutstandingBalance);
        Assert.Equal("Open", row.Status);
    }

    #endregion

    #region Credit Statement (Itemised)

    [Fact]
    public void GetCreditStatement_ReturnsItemisedLinesAndCalculatesTotals()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "testcashier", PasswordHash = "test-hash" };
        var rice = new Product { Name = "Rice", Barcode = "111", SellingPrice = 2000m, StockQuantity = 50 };
        var sugar = new Product { Name = "Sugar", Barcode = "222", SellingPrice = 1000m, StockQuantity = 50 };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.AddRange(rice, sugar);
        context.SaveChanges();

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

        context.CreditSaleLines.AddRange(
            new CreditSaleLine
            {
                CreditSaleId = sale.Id,
                ProductId = rice.Id,
                ProductName = "Rice",
                Barcode = "111",
                UnitPrice = 2000m,
                Quantity = 3,
                LineTotal = 6000m
            },
            new CreditSaleLine
            {
                CreditSaleId = sale.Id,
                ProductId = sugar.Id,
                ProductName = "Sugar",
                Barcode = "222",
                UnitPrice = 1000m,
                Quantity = 4,
                LineTotal = 4000m
            });

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

        Assert.Equal(2, result.Transactions.Count);
        Assert.Equal("Test Customer", result.CustomerName);
        Assert.Equal("Rice", result.Transactions[0].ProductName);
        Assert.Equal(6000m, result.Transactions[0].LineTotal);
        Assert.Equal("Sugar", result.Transactions[1].ProductName);
        Assert.Equal(4000m, result.Transactions[1].LineTotal);

        Assert.Equal(10000m, result.TotalPurchases);
        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(3500m, result.TotalPayments);
        Assert.Equal(7000m, result.TotalOutstanding);
    }

    [Fact]
    public void GetCreditStatement_ThrowsWhenCustomerDoesNotExist()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

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
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "testcashier", PasswordHash = "test-hash" };
        var product = new Product { Name = "Flour", Barcode = "333", SellingPrice = 2500m, StockQuantity = 50 };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.Add(product);
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

        context.CreditSaleLines.Add(new CreditSaleLine
        {
            CreditSaleId = sale.Id,
            ProductId = product.Id,
            ProductName = "Flour",
            Barcode = "333",
            UnitPrice = 2500m,
            Quantity = 4,
            LineTotal = 10000m
        });
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetCreditStatement(
            customer.Id,
            new DateTime(2026, 11, 10),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Transactions);
        Assert.Equal("Flour", row.ProductName);
        Assert.Equal(10000m, result.TotalPurchases);
        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(10500m, result.TotalOutstanding);
        Assert.Equal(new DateTime(2026, 11, 17), row.DueDate);
    }

    [Fact]
    public void GetCreditStatement_ExcludesPurchasePaidBeforeReportingPeriod()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "testcashier", PasswordHash = "test-hash" };
        var product = new Product { Name = "Flour", Barcode = "333", SellingPrice = 2500m, StockQuantity = 50 };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.Add(product);
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

        context.CreditSaleLines.Add(new CreditSaleLine
        {
            CreditSaleId = sale.Id,
            ProductId = product.Id,
            ProductName = "Flour",
            Barcode = "333",
            UnitPrice = 2500m,
            Quantity = 4,
            LineTotal = 10000m
        });

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

    #endregion

    #region Inventory Report

    [Fact]
    public void GetInventoryReport_ReturnsStockValuationAndConsignmentQuantities()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "inventorytest", PasswordHash = "test-hash" };

        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456",
            SellingPrice = 500m,
            StockQuantity = 20,
            UnitsPerCase = 12,
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
        Assert.Equal("123456", row.Barcode);
        Assert.Equal("Rice", row.ProductName);
        Assert.Equal(12, row.UnitsPerCase);
        Assert.Equal(20, row.AvailableStock);
        Assert.Equal(7, row.OutstandingConsignmentQuantity);
        Assert.Equal(5, row.MinimumStockLevel);
        Assert.Equal(500m, row.UnitSellingPrice);
        Assert.Equal(10000m, row.StockRetailValue);
        Assert.Equal("In Stock", row.StockStatus);
        Assert.False(row.IsLowStock);

        Assert.Equal(20, result.TotalUnitsInStock);
        Assert.Equal(7, result.TotalUnitsOnConsignment);
        Assert.Equal(10000m, result.TotalInventoryValue);
    }

    [Fact]
    public void GetInventoryReport_IdentifiesLowStockAndOutOfStockProducts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

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
                Name = "Salt",
                Barcode = "444444",
                SellingPrice = 150m,
                StockQuantity = 0,
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

        Assert.Equal(3, result.TotalProducts);
        Assert.Equal(2, result.LowStockProductCount);

        var sugar = result.Products.Single(p => p.ProductName == "Sugar");
        Assert.Equal("Low Stock", sugar.StockStatus);

        var salt = result.Products.Single(p => p.ProductName == "Salt");
        Assert.Equal("Out of Stock", salt.StockStatus);

        var flour = result.Products.Single(p => p.ProductName == "Flour");
        Assert.Equal("In Stock", flour.StockStatus);
    }

    [Fact]
    public void GetInventoryReport_ReturnsZeroOutstandingConsignmentQuantity()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

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

    #endregion

    #region Consignment Statement (Itemised)

    [Fact]
    public void GetConsignmentStatement_ReturnsItemisedLinesAndCalculatesOutstandingBalance()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "consignmenttest", PasswordHash = "test-hash" };
        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456789",
            SellingPrice = 500m,
            StockQuantity = 20,
            UnitsPerCase = 1,
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
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-001",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };
        context.Consignments.Add(consignment);
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

        Assert.Equal("Rice", row.ProductName);
        Assert.Equal(20, row.QuantityIssued);
        Assert.Equal(4, row.QuantityAcceptedReturned);
        Assert.Equal(16, row.RemainingQuantity);
        Assert.Equal(10000m, row.OriginalLineValue);
        Assert.Equal(2000m, row.AcceptedReturnsValue);
        Assert.Equal(8000m, row.NetGoodsValue);

        Assert.Equal(500m, result.TotalFees);
        Assert.Equal(2000m, result.TotalPayments);
        Assert.Equal(6500m, result.TotalOutstanding);
    }

    [Fact]
    public void GetConsignmentStatement_IncludesEarlierOutstandingConsignment()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "consignmenttest", PasswordHash = "test-hash" };
        var product = new Product
        {
            Name = "Sugar",
            Barcode = "987654",
            SellingPrice = 500m,
            StockQuantity = 30
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.Add(product);
        context.SaveChanges();

        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            IssuedAt = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-002",
            OriginalGoodsValue = 5000m,
            ConsignmentFee = 250m
        };
        context.Consignments.Add(consignment);
        context.SaveChanges();

        context.ConsignmentLines.Add(new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = "Sugar",
            Barcode = "987654",
            UnitPrice = 500m,
            Quantity = 10,
            LineTotal = 5000m
        });
        context.SaveChanges();

        var repository = new ReportingRepository(context);

        var result = repository.GetConsignmentStatement(
            customer.Id,
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        var row = Assert.Single(result.Transactions);

        Assert.Equal("CON-TEST-002", row.ReferenceNumber);
        Assert.Equal("Sugar", row.ProductName);
        Assert.Equal(5250m, result.TotalOutstanding);
    }

    [Fact]
    public void GetConsignmentStatement_ExcludesFullySettledEarlierConsignment()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "consignmenttest", PasswordHash = "test-hash" };
        var product = new Product
        {
            Name = "Sugar",
            Barcode = "987654",
            SellingPrice = 500m,
            StockQuantity = 30
        };

        context.Customers.Add(customer);
        context.Users.Add(user);
        context.Products.Add(product);
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

        context.ConsignmentLines.Add(new ConsignmentLine
        {
            ConsignmentId = consignment.Id,
            ProductId = product.Id,
            ProductName = "Sugar",
            Barcode = "987654",
            UnitPrice = 500m,
            Quantity = 10,
            LineTotal = 5000m
        });

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
        using var context = CreateContext(connection);

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
        using var context = CreateContext(connection);

        var customer = new Customer { Name = "Test Customer" };
        var user = new User { Username = "consignmenttest", PasswordHash = "test-hash" };
        var product = new Product
        {
            Name = "Rice",
            Barcode = "123456789",
            SellingPrice = 500m,
            StockQuantity = 20,
            UnitsPerCase = 1,
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
            IssuedAt = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            ReferenceNumber = "CON-TEST-004",
            OriginalGoodsValue = 10000m,
            ConsignmentFee = 500m
        };
        context.Consignments.Add(consignment);
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

        Assert.Equal(0, row.QuantityAcceptedReturned);
        Assert.Equal(20, row.RemainingQuantity);
        Assert.Equal(0m, row.AcceptedReturnsValue);
        Assert.Equal(0m, result.TotalPayments);
        Assert.Equal(10500m, result.TotalOutstanding);
    }

    #endregion
}