using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Services;
using Moq;

namespace KKWholesalePOS.Tests.Services;

public class ReportingServiceTests
{
    private readonly Mock<IReportingRepository> _repositoryMock;

    public ReportingServiceTests()
    {
        _repositoryMock = new Mock<IReportingRepository>();
    }

    private ReportingService CreateAuthorizedService()
    {
        var userSession = new UserSessionService();

        var user = new User
        {
            Id = 100,
            Username = "testadmin",
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Administrator",
                        RolePermissions = new List<RolePermission>
                        {
                            new RolePermission
                            {
                                Permission = new Permission
                                {
                                    Name = "Reports.View"
                                }
                            }
                        }
                    }
                }
            }
        };

        userSession.StartSession(user);

        return new ReportingService(
            _repositoryMock.Object,
            userSession,
            new AuthorizationService());
    }

    private ReportingService CreateUnauthorizedService()
    {
        return new ReportingService(
            _repositoryMock.Object,
            new UserSessionService(),
            new AuthorizationService());
    }

    [Fact]
    public void GetSalesReport_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        var startDate = new DateTime(2026, 11, 21);
        var endDate = new DateTime(2026, 11, 20);

        Assert.Throws<ArgumentException>(() =>
            service.GetSalesReport(startDate, endDate));

        _repositoryMock.Verify(
            repository => repository.GetSalesReport(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public void GetCreditReport_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetCreditReport(
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetConsignmentReport_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetConsignmentReport(
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetCreditReport_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetCreditReport(
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetConsignmentReport_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetConsignmentReport(
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetCreditStatement_InvalidCustomerId_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetCreditStatement(
                0,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetConsignmentStatement_InvalidCustomerId_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetConsignmentStatement(
                -1,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetCreditStatement_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetCreditStatement(
                1,
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetConsignmentStatement_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var service = CreateAuthorizedService();

        Assert.Throws<ArgumentException>(() =>
            service.GetConsignmentStatement(
                1,
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetSalesReport_ValidDates_PassesDateOnlyValuesToRepository()
    {
        var service = CreateAuthorizedService();

        var startDate = new DateTime(2026, 11, 1, 9, 30, 0);
        var endDate = new DateTime(2026, 11, 21, 17, 45, 0);

        _repositoryMock
            .Setup(repository => repository.GetSalesReport(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new SalesReportResult());

        service.GetSalesReport(startDate, endDate);

        _repositoryMock.Verify(
            repository => repository.GetSalesReport(
                startDate.Date, endDate.Date),
            Times.Once);
    }

    [Fact]
    public void GetSalesReport_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        var service = CreateUnauthorizedService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetSalesReport(
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));

        _repositoryMock.Verify(
            repository => repository.GetSalesReport(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }
}