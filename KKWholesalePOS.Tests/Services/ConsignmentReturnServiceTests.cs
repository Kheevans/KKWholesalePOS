
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Services;
using KKWholesalePOS.Data;
using KKWholesalePOS.Data.Repositories;
using KKWholesalePOS.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KKWholesalePOS.Tests.Services;

public class ConsignmentReturnServiceTests
{
    private sealed class TestSetup : IDisposable
    {
        public SqliteConnection Connection { get; }
        public AppDbContext Context { get; }
        public ConsignmentReturnService Service { get; }
        public Consignment Consignment { get; }
        public User User { get; }
        public Product Product { get; }

        public TestSetup(
            SqliteConnection connection,
            AppDbContext context,
            ConsignmentReturnService service,
            Consignment consignment,
            User user,
            Product product)
        {
            Connection = connection;
            Context = context;
            Service = service;
            Consignment = consignment;
            User = user;
            Product = product;
        }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }

    private static TestSetup CreateSetup(
        bool grantPermission = true,
        bool startSession = true)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        // Create user
        var user = new User
        {
            Username = "testcashier",
            PasswordHash = "test-hash"
        };

        context.Users.Add(user);

        // Create permission and assign it through a role
        if (grantPermission)
        {
            var permission = new Permission
            {
                Name = "Consignments.Return",
                Description = "Process consignment returns and payments"
            };

            var role = new Role
            {
                Name = "Cashier",
                Description = "Cashier role"
            };

            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = role
            });

            role.RolePermissions.Add(new RolePermission
            {
                Role = role,
                Permission = permission
            });

            context.Roles.Add(role);
            context.Permissions.Add(permission);
        }

        context.SaveChanges();

        // Create customer
        var customer = new Customer
        {
            Name = "Test Customer",
            PhoneNumber = "1234567890",
            Address = "Test Address"
        };

        context.Customers.Add(customer);

        // Create product
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

        // Create consignment
        var consignment = new Consignment
        {
            CustomerId = customer.Id,
            UserId = user.Id,
            ReferenceNumber = "CON-TEST-001",
            OriginalGoodsValue = 100m,
            ConsignmentFee = 5m,
            Lines = new List<ConsignmentLine>
            {
                new ConsignmentLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Barcode = product.Barcode,
                    UnitPrice = 20m,
                    Quantity = 5,
                    LineTotal = 100m
                }
            }
        };

        context.Consignments.Add(consignment);
        context.SaveChanges();

        // Set up repository and services
        IConsignmentReturnRepository repository =
            new ConsignmentReturnRepository(
                context);

        var userSession = new UserSessionService();

        if (startSession)
            userSession.StartSession(user);

        var authorizationService = new AuthorizationService();

        var service = new ConsignmentReturnService(
            repository,
            userSession,
            authorizationService);

        return new TestSetup(
            connection,
            context,
            service,
            consignment,
            user,
            product);
    }

    // Authorization tests

    [Fact]
    public void FindConsignment_ThrowsException_WhenUserIsNotAuthenticated()
    {
        using var setup = CreateSetup(startSession: false);

        Assert.Throws<UnauthorizedAccessException>(
            () => setup.Service.FindConsignment("CON-TEST-001"));
    }

    [Fact]
    public void FindConsignment_ThrowsException_WhenUserDoesNotHavePermission()
    {
        using var setup = CreateSetup(grantPermission: false);

        Assert.Throws<UnauthorizedAccessException>(
            () => setup.Service.FindConsignment("CON-TEST-001"));
    }

    // Find consignment tests

    [Fact]
    public void FindConsignment_ReturnsConsignment_WhenReferenceExists()
    {
        using var setup = CreateSetup();

        var result = setup.Service.FindConsignment("CON-TEST-001");

        Assert.NotNull(result);
        Assert.Equal("CON-TEST-001", result.Consignment.ReferenceNumber);
        Assert.Equal(100m, result.Settlement.OriginalTotal);
        Assert.Equal(5m, result.Settlement.Fee);
        Assert.Equal(105m, result.Settlement.FinalBalance);
        Assert.Equal(105m, result.Settlement.Outstanding);
        Assert.False(result.Settlement.IsSettled);
    }

    [Fact]
    public void FindConsignment_TrimsReferenceNumber()
    {
        using var setup = CreateSetup();

        var result = setup.Service.FindConsignment("  CON-TEST-001  ");

        Assert.NotNull(result);
        Assert.Equal("CON-TEST-001", result.Consignment.ReferenceNumber);
    }

    [Fact]
    public void FindConsignment_ReturnsNull_WhenReferenceDoesNotExist()
    {
        using var setup = CreateSetup();

        var result = setup.Service.FindConsignment("INVALID-REF");

        Assert.Null(result);
    }

    [Fact]
    public void FindConsignment_ThrowsException_WhenReferenceIsBlank()
    {
        using var setup = CreateSetup();

        Assert.Throws<ArgumentException>(
            () => setup.Service.FindConsignment(" "));
    }

    // Customer consignment tests

    [Fact]
    public void GetCustomerConsignments_ReturnsCustomerConsignmentSummary()
    {
        using var setup = CreateSetup();

        var result = setup.Service.GetCustomerConsignments(
            setup.Consignment.CustomerId);

        var summary = Assert.Single(result);

        Assert.Equal(setup.Consignment.Id, summary.ConsignmentId);
        Assert.Equal("CON-TEST-001", summary.ReferenceNumber);
        Assert.Equal(105m, summary.FinalBalance);
        Assert.Equal(105m, summary.Outstanding);
        Assert.False(summary.IsSettled);
        Assert.Contains("CON-TEST-001", summary.DisplayText);
    }

    [Fact]
    public void GetCustomerConsignments_ThrowsException_WhenCustomerIdIsInvalid()
    {
        using var setup = CreateSetup();

        Assert.Throws<ArgumentException>(
            () => setup.Service.GetCustomerConsignments(0));
    }

    // Return history tests

    [Fact]
    public void GetReturnHistory_ReturnsSavedReturns()
    {
        using var setup = CreateSetup();

        var repository = new ConsignmentReturnRepository(
            setup.Context);

        repository.SaveReturn(new ConsignmentReturn
        {
            ConsignmentId = setup.Consignment.Id,
            UserId = setup.User.Id,
            ReferenceNumber = "RET-TEST-001",
            Lines = new List<ConsignmentReturnLine>
            {
                new ConsignmentReturnLine
                {
                    ConsignmentLineId = setup.Consignment.Lines.First().Id,
                    QuantityReturned = 2,
                    QuantityAccepted = 2,
                    UnitPrice = 20m
                }
            }
        });

        var result = setup.Service.GetReturnHistory(
            setup.Consignment.Id);

        var savedReturn = Assert.Single(result);

        Assert.Equal("RET-TEST-001", savedReturn.ReferenceNumber);
        Assert.Single(savedReturn.Lines);
    }

    // Payment history tests

    [Fact]
    public void GetPaymentHistory_ReturnsSavedPayments()
    {
        using var setup = CreateSetup();

        var repository = new ConsignmentReturnRepository(
            setup.Context);

        repository.SavePayment(new ConsignmentPayment
        {
            ConsignmentId = setup.Consignment.Id,
            UserId = setup.User.Id,
            Amount = 25m,
            PaymentMethod = "Cash"
        });

        var result = setup.Service.GetPaymentHistory(
            setup.Consignment.Id);

        var payment = Assert.Single(result);

        Assert.Equal(25m, payment.Amount);
        Assert.Equal("Cash", payment.PaymentMethod);
    }

    // Process return tests

    [Fact]
    public void ProcessReturn_SavesReturn_WhenRequestIsValid()
    {
        using var setup = CreateSetup();

        var lineId = setup.Consignment.Lines.First().Id;

        var result = setup.Service.ProcessReturn(
            "CON-TEST-001",
            new List<ReturnRequestLine>
            {
                new ReturnRequestLine(lineId, 2, 2)
            });

        Assert.NotNull(result);
        Assert.Equal(setup.Consignment.Id, result.ConsignmentId);
        Assert.Equal(setup.User.Id, result.UserId);

        var savedLine = Assert.Single(result.Lines);

        Assert.Equal(2, savedLine.QuantityReturned);
        Assert.Equal(2, savedLine.QuantityAccepted);
        Assert.Equal(20m, savedLine.UnitPrice);
    }

    [Fact]
    public void ProcessReturn_UpdatesInventory_WhenItemsAreAccepted()
    {
        using var setup = CreateSetup();

        var lineId = setup.Consignment.Lines.First().Id;

        setup.Service.ProcessReturn(
            "CON-TEST-001",
            new List<ReturnRequestLine>
            {
                new ReturnRequestLine(lineId, 2, 2)
            });

        setup.Context.Entry(setup.Product).Reload();

        Assert.Equal(12, setup.Product.StockQuantity);
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenConsignmentDoesNotExist()
    {
        using var setup = CreateSetup();

        Assert.Throws<InvalidOperationException>(
            () => setup.Service.ProcessReturn(
                "INVALID-REF",
                new List<ReturnRequestLine>
                {
                    new ReturnRequestLine(1, 1, 1)
                }));
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenNoItemsAreProvided()
    {
        using var setup = CreateSetup();

        Assert.Throws<ArgumentException>(
            () => setup.Service.ProcessReturn(
                "CON-TEST-001",
                new List<ReturnRequestLine>()));
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenReturnedQuantityIsTooHigh()
    {
        using var setup = CreateSetup();

        var lineId = setup.Consignment.Lines.First().Id;

        Assert.Throws<InvalidOperationException>(
            () => setup.Service.ProcessReturn(
                "CON-TEST-001",
                new List<ReturnRequestLine>
                {
                    new ReturnRequestLine(lineId, 6, 6)
                }));
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenAcceptedQuantityExceedsReturnedQuantity()
    {
        using var setup = CreateSetup();

        var lineId = setup.Consignment.Lines.First().Id;

        Assert.Throws<ArgumentException>(
            () => setup.Service.ProcessReturn(
                "CON-TEST-001",
                new List<ReturnRequestLine>
                {
                    new ReturnRequestLine(lineId, 2, 3)
                }));
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenReturnContainsDuplicateLines()
    {
        using var setup = CreateSetup();

        var lineId = setup.Consignment.Lines.First().Id;

        Assert.Throws<ArgumentException>(
            () => setup.Service.ProcessReturn(
                "CON-TEST-001",
                new List<ReturnRequestLine>
                {
                    new ReturnRequestLine(lineId, 1, 1),
                    new ReturnRequestLine(lineId, 1, 1)
                }));
    }

    [Fact]
    public void ProcessReturn_ThrowsException_WhenReturnWouldCauseOverpayment()
    {
        using var setup = CreateSetup();

        var repository = new ConsignmentReturnRepository(
            setup.Context);

        // The original balance is $105.
        // Paying $100 leaves $5 outstanding.
        repository.SavePayment(new ConsignmentPayment
        {
            ConsignmentId = setup.Consignment.Id,
            UserId = setup.User.Id,
            Amount = 100m,
            PaymentMethod = "Cash"
        });

        // Returning one accepted item worth $20 would
        // reduce the final balance to $85.
        // This would result in a $15 overpayment.

        var lineId = setup.Consignment.Lines.First().Id;

        Assert.Throws<InvalidOperationException>(
            () => setup.Service.ProcessReturn(
                "CON-TEST-001",
                new List<ReturnRequestLine>
                {
                    new ReturnRequestLine(lineId, 1, 1)
                }));

        Assert.Empty(setup.Context.ConsignmentReturns);
    }

    // Record payment tests

    [Fact]
    public void RecordPayment_SavesPayment_WhenAmountIsValid()
    {
        using var setup = CreateSetup();

        var result = setup.Service.RecordPayment(
            "CON-TEST-001",
            25m,
            "Cash");

        Assert.NotNull(result);
        Assert.Equal(25m, result.Amount);
        Assert.Equal("Cash", result.PaymentMethod);
        Assert.Equal(setup.User.Id, result.UserId);
        Assert.Equal(setup.Consignment.Id, result.ConsignmentId);
    }

    [Fact]
    public void RecordPayment_RoundsAmountToTwoDecimalPlaces()
    {
        using var setup = CreateSetup();

        var result = setup.Service.RecordPayment(
            "CON-TEST-001",
            10.126m,
            "Cash");

        Assert.Equal(10.13m, result.Amount);
    }

    [Fact]
    public void RecordPayment_UsesCash_WhenPaymentMethodIsBlank()
    {
        using var setup = CreateSetup();

        var result = setup.Service.RecordPayment(
            "CON-TEST-001",
            25m,
            " ");

        Assert.Equal("Cash", result.PaymentMethod);
    }

    [Fact]
    public void RecordPayment_TrimsPaymentMethod()
    {
        using var setup = CreateSetup();

        var result = setup.Service.RecordPayment(
            "CON-TEST-001",
            25m,
            "  Card  ");

        Assert.Equal("Card", result.PaymentMethod);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void RecordPayment_ThrowsException_WhenAmountIsNotPositive(
        decimal amount)
    {
        using var setup = CreateSetup();

        Assert.Throws<ArgumentException>(
            () => setup.Service.RecordPayment(
                "CON-TEST-001",
                amount,
                "Cash"));
    }

    [Fact]
    public void RecordPayment_ThrowsException_WhenPaymentExceedsOutstandingBalance()
    {
        using var setup = CreateSetup();

        Assert.Throws<InvalidOperationException>(
            () => setup.Service.RecordPayment(
                "CON-TEST-001",
                106m,
                "Cash"));

        Assert.Empty(setup.Context.ConsignmentPayments);
    }
}
