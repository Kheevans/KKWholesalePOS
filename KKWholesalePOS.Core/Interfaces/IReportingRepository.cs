using KKWholesalePOS.Core.Models.Reports;

namespace KKWholesalePOS.Core.Interfaces;

public interface IReportingRepository
{
    SalesReportResult GetSalesReport(
        DateTime startDate,
        DateTime endDate);

    CreditReportResult GetCreditReport(
        DateTime startDate,
        DateTime endDate);

    ConsignmentReportResult GetConsignmentReport(
        DateTime startDate,
        DateTime endDate);

    CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate,
        string? referenceNumber = null);

    ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate,
        string? referenceNumber = null);

    InventoryReportResult GetInventoryReport();
}