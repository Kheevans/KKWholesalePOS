using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Sales;
using KKWholesalePOS.Core.Services;
using Moq;

namespace KKWholesalePOS.Tests;

public class CreditServiceTests
{
    private readonly Mock<ICreditRepository> _repository = new();
    private readonly Mock<IAuditLogRepository> _auditRepository = new();

    private CreditService CreateAuthorizedService(params string[] permissions)
    {
        var session = new UserSessionService();

        var rolePermissions = permissions
            .Select(p => new RolePermission
            {
                Permission = new Permission { Name = p }
            })
            .ToList();

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "TestRole",
                        RolePermissions = rolePermissions
                    }
                }
            }
        };

        session.StartSession(user);

        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            _auditRepository.Object,
            session,
            authService);

        return new CreditService(
            _repository.Object,
            session,
            authService,
            auditService);
    }

    private CreditService CreateUnauthorizedService()
    {
        var session = new UserSessionService();
        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            _auditRepository.Object,
            session,
            authService);

        return new CreditService(
            _repository.Object,
            session,
            authService,
            auditService);
    }

    private static CreditSale CreateSale()
    {
        return new CreditSale
        {
            Id = 1,
            CustomerId = 1,
            UserId = 1,
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            ReferenceNumber = "CR-001",
            DueDate = DateTime.UtcNow.AddDays(14)
        };
    }

    [Fact]
    public void CreateCreditSale_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.CreateCreditSale(1, new List<SaleRequestLine> { new(1, 1) }));

        _repository.Verify(
            r => r.CreateCreditSaleWithStock(It.IsAny<CreditSale>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void CreateCreditSale_AddsFivePercentFee_AndLogsAuditEntry()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Barcode = "12345",
            SellingPrice = 1000m,
            StockQuantity = 10
        };

        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(false);
        _repository.Setup(r => r.GetProductById(1)).Returns(product);

        var service = CreateAuthorizedService("Credit.Manage");

        var sale = service.CreateCreditSale(
            1,
            new List<SaleRequestLine> { new(1, 2) });

        Assert.Equal(2000m, sale.PurchaseAmount);
        Assert.Equal(100m, sale.CreditFee);
        Assert.Equal(2100m, sale.TotalAmount);

        _repository.Verify(
            r => r.CreateCreditSaleWithStock(It.IsAny<CreditSale>(), 1),
            Times.Once);

        _auditRepository.Verify(
            a => a.Add(It.Is<AuditLogEntry>(e =>
                e.Action == "CreditSaleCreated" &&
                e.EntityType == "CreditSale" &&
                e.UserId == 1)),
            Times.Once);
    }

    [Fact]
    public void CreateCreditSale_RejectsInvalidQuantity()
    {
        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(false);

        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1,
                new List<SaleRequestLine> { new(1, 0) }));

        _repository.Verify(
            r => r.CreateCreditSaleWithStock(It.IsAny<CreditSale>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void CreateCreditSale_RejectsQuantityGreaterThanStock()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            SellingPrice = 1000m,
            StockQuantity = 2
        };

        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(false);
        _repository.Setup(r => r.GetProductById(1)).Returns(product);

        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1,
                new List<SaleRequestLine> { new(1, 3) }));

        _repository.Verify(
            r => r.CreateCreditSaleWithStock(It.IsAny<CreditSale>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void CreateCreditSale_RejectsCustomerWithUnpaidCredit()
    {
        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(true);

        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1,
                new List<SaleRequestLine> { new(1, 1) }));

        _repository.Verify(
            r => r.CreateCreditSaleWithStock(It.IsAny<CreditSale>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void CreateCreditSale_RejectsPurchaseExceedingCreditLimit()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Bulk Liquor",
            SellingPrice = 60000m,
            StockQuantity = 10
        };

        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(false);
        _repository.Setup(r => r.GetProductById(1)).Returns(product);

        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1,
                new List<SaleRequestLine> { new(1, 2) }));
    }

    [Fact]
    public void RecordPayment_AcceptsPartialPayment_AndLogsAuditEntry()
    {
        var sale = CreateSale();

        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateAuthorizedService("Credit.Manage");

        var payment = service.RecordPayment(1, 3000m, "Cash", "RCPT-100");

        Assert.Equal(3000m, payment.Amount);
        Assert.Equal("Cash", payment.PaymentMethod);

        _repository.Verify(
            r => r.AddPaymentWithinBalance(It.IsAny<CreditPayment>()),
            Times.Once);

        _auditRepository.Verify(
            a => a.Add(It.Is<AuditLogEntry>(e =>
                e.Action == "CreditPaymentRecorded" &&
                e.EntityType == "CreditPayment")),
            Times.Once);
    }

    [Fact]
    public void RecordPayment_RejectsExcessivePayment()
    {
        var sale = CreateSale();

        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<InvalidOperationException>(() =>
            service.RecordPayment(1, 11000m, "Cash"));

        _repository.Verify(
            r => r.AddPaymentWithinBalance(It.IsAny<CreditPayment>()),
            Times.Never);
    }

    [Fact]
    public void ModifyCreditAmount_WithoutModifyPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateAuthorizedService("Credit.Manage");

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.ModifyCreditAmount(1, 8000m, "Correction"));
    }

    [Fact]
    public void ModifyCreditAmount_RequiresReason()
    {
        var service = CreateAuthorizedService("Credit.Modify");

        Assert.Throws<ArgumentException>(() =>
            service.ModifyCreditAmount(1, 8000m, ""));
    }

    [Fact]
    public void ModifyCreditAmount_WithPermissionAndReason_UpdatesAndLogsAudit()
    {
        var sale = CreateSale();
        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateAuthorizedService("Credit.Modify");

        service.ModifyCreditAmount(1, 8000m, "Invoice correction approved by owner");

        Assert.Equal(8000m, sale.PurchaseAmount);
        Assert.Equal(400m, sale.CreditFee);

        _repository.Verify(r => r.UpdateCreditSale(sale), Times.Once);
        _auditRepository.Verify(
            a => a.Add(It.Is<AuditLogEntry>(e =>
                e.Action == "CreditAmountModified" &&
                e.Details!.Contains("Invoice correction approved by owner"))),
            Times.Once);
    }

    [Fact]
    public void GetCustomerCreditSummary_DetectsOverdueBalance()
    {
        var overdueSale = new CreditSale
        {
            Id = 1,
            CustomerId = 1,
            PurchaseAmount = 10000m,
            CreditFee = 500m,
            DueDate = DateTime.UtcNow.AddDays(-2)
        };

        _repository.Setup(r => r.GetByCustomerId(1))
            .Returns(new List<CreditSale> { overdueSale });
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>
            {
                new CreditPayment { Amount = 2500m }
            });

        var service = CreateAuthorizedService("Credit.Manage");
        var summary = service.GetCustomerCreditSummary(1);

        Assert.Equal(10500m, summary.TotalPurchasedWithFees);
        Assert.Equal(2500m, summary.TotalPaid);
        Assert.Equal(8000m, summary.OutstandingBalance);
        Assert.True(summary.HasUnpaidInvoice);
        Assert.True(summary.HasOverdueInvoice);
    }
}