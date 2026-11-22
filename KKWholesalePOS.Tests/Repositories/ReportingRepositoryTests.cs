
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

}