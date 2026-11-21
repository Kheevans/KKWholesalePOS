using KKWholesalePOS.Core.Models.Reports;

namespace KKWholesalePOS.Core.Interfaces;

public interface IReportingRepository
{
    SalesReportResult GetSalesReport(
        DateTime startDate,
        DateTime endDate);

    CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate);

    ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate);

    InventoryReportResult GetInventoryReport();
}