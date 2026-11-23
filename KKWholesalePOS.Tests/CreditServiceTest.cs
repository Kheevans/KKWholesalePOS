
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

    private CreditService CreateService()
    {
        return new CreditService(
            _repository.Object,
            _auditRepository.Object);
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
    public void CreateCreditSale_AddsFivePercentFee()
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

        var service = CreateService();

        var sale = service.CreateCreditSale(
            1, 1, new List<SaleRequestLine>
            {
                new(1, 2)
            });

        Assert.Equal(2000m, sale.PurchaseAmount);
        Assert.Equal(100m, sale.CreditFee);
        Assert.Equal(2100m, sale.TotalAmount);
        _repository.Verify(r => r.AddCreditSale(It.IsAny<CreditSale>()), Times.Once);
    }

    [Fact]
    public void CreateCreditSale_RejectsInvalidQuantity()
    {
        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(false);

        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1, 1, new List<SaleRequestLine>
                {
                    new(1, 0)
                }));

        _repository.Verify(
            r => r.AddCreditSale(It.IsAny<CreditSale>()),
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

        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1, 1, new List<SaleRequestLine>
                {
                    new(1, 3)
                }));

        _repository.Verify(
            r => r.AddCreditSale(It.IsAny<CreditSale>()),
            Times.Never);
    }

    [Fact]
    public void CreateCreditSale_RejectsCustomerWithUnpaidCredit()
    {
        _repository.Setup(r => r.HasUnpaidCredit(1)).Returns(true);

        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateCreditSale(
                1, 1, new List<SaleRequestLine>
                {
                    new(1, 1)
                }));

        _repository.Verify(
            r => r.AddCreditSale(It.IsAny<CreditSale>()),
            Times.Never);
    }

    [Fact]
    public void RecordPayment_AcceptsPartialPayment()
    {
        var sale = CreateSale();

        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateService();

        var payment = service.RecordPayment(
            1, 1, 3000m, "Cash");

        Assert.Equal(3000m, payment.Amount);
        Assert.Equal(7500m, sale.TotalAmount - payment.Amount);

        _repository.Verify(
            r => r.AddPayment(It.IsAny<CreditPayment>()),
            Times.Once);
    }

    [Fact]
    public void RecordPayment_RejectsPaymentGreaterThanBalance()
    {
        var sale = CreateSale();

        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.RecordPayment(1, 1, 11000m, "Cash"));

        _repository.Verify(
            r => r.AddPayment(It.IsAny<CreditPayment>()),
            Times.Never);
    }

    [Fact]
    public void ModifyCreditAmount_AllowsOwnerAndCreatesAuditLog()
    {
        var sale = CreateSale();

        var owner = new User
        {
            Id = 1,
            Username = "owner",
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = 1,
                    RoleId = 1,
                    Role = new Role { Id = 1, Name = "Owner" }
                }
            }
        };

        _repository.Setup(r => r.GetUserWithRoles(1)).Returns(owner);
        _repository.Setup(r => r.GetById(1)).Returns(sale);
        _repository.Setup(r => r.GetPaymentsByCreditSaleId(1))
            .Returns(new List<CreditPayment>());

        var service = CreateService();

        service.ModifyCreditAmount(1, 1, 12000m);

        Assert.Equal(12000m, sale.PurchaseAmount);
        Assert.Equal(600m, sale.CreditFee);

        _repository.Verify(r => r.UpdateCreditSale(sale), Times.Once);
        _auditRepository.Verify(
            r => r.Add(It.IsAny<AuditLogEntry>()),
            Times.Once);
    }

    [Fact]
    public void ModifyCreditAmount_RejectsUnauthorizedStaff()
    {
        var staff = new User
        {
            Id = 2,
            Username = "staff",
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = 2,
                    RoleId = 2,
                    Role = new Role { Id = 2, Name = "Staff" }
                }
            }
        };

        _repository.Setup(r => r.GetUserWithRoles(2)).Returns(staff);

        var service = CreateService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.ModifyCreditAmount(1, 2, 12000m));

        _repository.Verify(
            r => r.UpdateCreditSale(It.IsAny<CreditSale>()),
            Times.Never);
    }
}
