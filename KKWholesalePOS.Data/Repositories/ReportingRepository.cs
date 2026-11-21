
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Data;

namespace KKWholesalePOS.Data.Repositories;

public class ReportingRepository : IReportingRepository
{
    private readonly AppDbContext _dbContext;

    public ReportingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SalesReportResult GetSalesReport(
        DateTime startDate,
        DateTime endDate)
    {
        throw new NotImplementedException(
            "Sales reporting will be implemented after the sales module is integrated.");
    }

    public CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        throw new NotImplementedException(
            "Credit reporting will be implemented after the credit module is integrated.");
    }

    public ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        throw new NotImplementedException(
            "Consignment reporting will be implemented after the consignment module is integrated.");
    }

    public InventoryReportResult GetInventoryReport()
    {
        throw new NotImplementedException(
            "Inventory reporting will be implemented after the inventory module is integrated.");
    }
}
