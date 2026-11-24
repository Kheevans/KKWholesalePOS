using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Sales;
using KKWholesalePOS.Core.Services;
using Moq;

namespace KKWholesalePOS.Tests.Services;

public class RegularSaleServiceTests
{
    private readonly Mock<IRegularSaleRepository> _repositoryMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepositoryMock = new();

    private RegularSaleService CreateAuthorizedService()
    {
        var session = new UserSessionService();

        var user = new User
        {
            Id = 1,
            Username = "testcashier",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Cashier",
                        RolePermissions = new List<RolePermission>
                        {
                            new RolePermission
                            {
                                Permission = new Permission
                                {
                                    Name = "Sales.Process"
                                }
                            }
                        }
                    }
                }
            }
        };

        session.StartSession(user);

        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            _auditRepositoryMock.Object,
            session,
            authService);

        return new RegularSaleService(
            _repositoryMock.Object,
            session,
            authService,
            auditService);
    }

    private RegularSaleService CreateUnauthorizedService()
    {
        var session = new UserSessionService();
        var authService = new AuthorizationService();
        var auditService = new AuditLogService(
            _auditRepositoryMock.Object,
            session,
            authService);

        return new RegularSaleService(
            _repositoryMock.Object,
            session,
            authService,
            auditService);
    }

    [Fact]
    public void FindProduct_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.FindProduct("123456"));

        _repositoryMock.Verify(
            repository => repository.FindProduct(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void CompleteCashSale_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        var items = new List<SaleRequestLine>
        {
            new SaleRequestLine(1, 2)
        };

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.CompleteCashSale(
                items,
                1000m,
                1,
                "Test Cashier"));

        _repositoryMock.Verify(
            repository => repository.SaveCashSale(
                It.IsAny<IReadOnlyList<SaleRequestLine>>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void CompleteCardSale_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        var items = new List<SaleRequestLine>
        {
            new SaleRequestLine(1, 2)
        };

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.CompleteCardSale(
                items,
                "Visa",
                "1234",
                "APPROVAL123",
                1,
                "Test Cashier"));

        _repositoryMock.Verify(
            repository => repository.SaveCardSale(
                It.IsAny<IReadOnlyList<SaleRequestLine>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void FindProduct_WithPermission_CallsRepository()
    {
        var service = CreateAuthorizedService();

        var expectedProduct = new Product
        {
            Id = 1,
            Name = "Test Product",
            Barcode = "123456",
            SellingPrice = 100m,
            StockQuantity = 10
        };

        _repositoryMock
            .Setup(repository => repository.FindProduct("123456"))
            .Returns(expectedProduct);

        var result = service.FindProduct("123456");

        Assert.Same(expectedProduct, result);

        _repositoryMock.Verify(
            repository => repository.FindProduct("123456"),
            Times.Once);
    }
}