
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Credit;
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
        DateTime startLocal = DateTime.SpecifyKind(
            startDate.Date, DateTimeKind.Local);

        DateTime endLocalExclusive = DateTime.SpecifyKind(
            endDate.Date.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocalExclusive.ToUniversalTime();

        string? customerName = _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => c.Name)
            .SingleOrDefault();

        if (customerName == null)
        {
            throw new InvalidOperationException(
                "The selected customer could not be found.");
        }

        // Retrieve purchases issued before the end of the period.
        var sales = _dbContext.CreditSales
            .AsNoTracking()
            .Where(s =>
                s.CustomerId == customerId &&
                s.IssuedAt < endUtc)
            .OrderBy(s => s.IssuedAt)
            .Select(s => new
            {
                s.Id,
                s.IssuedAt,
                s.DueDate,
                s.ReferenceNumber,
                s.PurchaseAmount,
                s.CreditFee
            })
            .ToList();

        var saleIds = sales.Select(s => s.Id).ToList();

        var payments = saleIds.Count == 0
            ? new List<CreditPayment>()
            : _dbContext.CreditPayments
                .AsNoTracking()
                .Where(p =>
                    saleIds.Contains(p.CreditSaleId) &&
                    p.PaymentDate < endUtc)
                .ToList();

        var paymentTotals = payments
            .GroupBy(p => p.CreditSaleId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(p => p.Amount));

        var rows = sales
            .Select(sale =>
            {
                paymentTotals.TryGetValue(
                    sale.Id, out decimal totalPayments);

                return new CreditStatementRow
                {
                    CreditSaleId = sale.Id,
                    TransactionDate = sale.IssuedAt.ToLocalTime(),
                    DueDate = sale.DueDate,
                    ReferenceNumber = sale.ReferenceNumber,
                    PurchaseAmount = sale.PurchaseAmount,
                    CreditFee = sale.CreditFee,
                    Payments = totalPayments
                };
            })
            .Where(row =>
                row.TransactionDate.Date >= startDate.Date ||
                row.OutstandingBalance > 0)
            .ToList();

        return new CreditStatementResult
        {
            CustomerId = customerId,
            CustomerName = customerName,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Transactions = rows
        };
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
