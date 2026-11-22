
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Core.Services;
using Moq;
using System.Timers;

namespace KKWholesalePOS.Tests.Services;

public class ReportingServiceTests
{
    private readonly Mock<IReportingRepository> _repositoryMock;
    private readonly ReportingService _reportingService;

    public ReportingServiceTests()
    {
        _repositoryMock = new Mock<IReportingRepository>();
        _reportingService = new ReportingService(_repositoryMock.Object);
    }

    [Fact]
    public void GetSalesReport_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var startDate = new DateTime(2026, 11, 21);
        var endDate = new DateTime(2026, 11, 20);

        Assert.Throws<ArgumentException>(() =>
            _reportingService.GetSalesReport(startDate, endDate));

        _repositoryMock.Verify(
            repository => repository.GetSalesReport(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public void GetCreditStatement_InvalidCustomerId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _reportingService.GetCreditStatement(
                0,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetConsignmentStatement_InvalidCustomerId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _reportingService.GetConsignmentStatement(
                -1,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void GetCreditStatement_StartDateAfterEndDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _reportingService.GetCreditStatement(
                1,
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetConsignmentStatement_StartDateAfterEndDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _reportingService.GetConsignmentStatement(
                1,
                new DateTime(2026, 11, 21),
                new DateTime(2026, 11, 20)));
    }

    [Fact]
    public void GetSalesReport_ValidDates_PassesDateOnlyValuesToRepository()
    {
        var startDate = new DateTime(2026, 11, 1, 9, 30, 0);
        var endDate = new DateTime(2026, 11, 21, 17, 45, 0);

        _repositoryMock
            .Setup(repository => repository.GetSalesReport(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new SalesReportResult());

        _reportingService.GetSalesReport(startDate, endDate);

        _repositoryMock.Verify(
            repository => repository.GetSalesReport(
                startDate.Date, endDate.Date),
            Times.Once);
    }
}
