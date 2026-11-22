
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Reports;
using KKWholesalePOS.Data;
using Microsoft.EntityFrameworkCore;

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
        // The UI selects dates in local time.
        // Convert the date boundaries to UTC because CompletedAt is stored in UTC.
        DateTime startLocal = DateTime.SpecifyKind(
            startDate.Date, DateTimeKind.Local);

        DateTime endLocalExclusive = DateTime.SpecifyKind(
            endDate.Date.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocalExclusive.ToUniversalTime();

        var sales = _dbContext.RegularSales
            .AsNoTracking()
            .Where(s =>
                s.CompletedAt >= startUtc &&
                s.CompletedAt < endUtc)
            .OrderBy(s => s.CompletedAt)
            .Select(s => new
            {
                s.Id,
                s.CompletedAt,
                s.CashierName,
                s.Total
            })
            .ToList();

        var rows = sales.Select(sale => new SalesReportRow
        {
            SaleId = sale.Id,
            ReceiptNumber = $"SALE-{sale.Id:D6}",
            SaleDate = sale.CompletedAt.ToLocalTime(),
            CashierName = sale.CashierName,
            TotalAmount = sale.Total
        }).ToList();

        return new SalesReportResult
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Transactions = rows
        };
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
