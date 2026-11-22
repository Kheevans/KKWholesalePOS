using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Reports;

namespace KKWholesalePOS.Core.Services;

public class ReportingService
{
    private readonly IReportingRepository _reportingRepository;

    public ReportingService(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public SalesReportResult GetSalesReport(
        DateTime startDate,
        DateTime endDate)
    {
        ValidateDateRange(startDate, endDate);

        return _reportingRepository.GetSalesReport(
            startDate.Date,
            endDate.Date);
    }

    public CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        ValidateCustomerAndDateRange(
            customerId, startDate, endDate);

        return _reportingRepository.GetCreditStatement(
            customerId,
            startDate.Date,
            endDate.Date);
    }

    public ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        ValidateCustomerAndDateRange(
            customerId, startDate, endDate);

        return _reportingRepository.GetConsignmentStatement(
            customerId,
            startDate.Date,
            endDate.Date);
    }

    public InventoryReportResult GetInventoryReport()
    {
        return _reportingRepository.GetInventoryReport();
    }

    private static void ValidateCustomerAndDateRange(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException(
                "A valid customer must be selected.",
                nameof(customerId));
        }

        ValidateDateRange(startDate, endDate);
    }

    private static void ValidateDateRange(
        DateTime startDate,
        DateTime endDate)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException(
                "The start date cannot be later than the end date.");
        }
    }
}