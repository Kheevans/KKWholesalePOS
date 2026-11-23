
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
        // Convert local date boundaries to UTC.
        DateTime startLocal = DateTime.SpecifyKind(
            startDate.Date, DateTimeKind.Local);

        DateTime endLocalExclusive = DateTime.SpecifyKind(
            endDate.Date.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocalExclusive.ToUniversalTime();

        // Retrieve customer information.
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

        // Retrieve consignments issued before the end of the period.
        var consignments = _dbContext.Consignments
            .AsNoTracking()
            .Where(c =>
                c.CustomerId == customerId &&
                c.IssuedAt < endUtc)
            .OrderBy(c => c.IssuedAt)
            .Select(c => new
            {
                c.Id,
                c.IssuedAt,
                c.ReferenceNumber,
                c.OriginalGoodsValue,
                c.ConsignmentFee
            })
            .ToList();

        var consignmentIds = consignments
            .Select(c => c.Id)
            .ToList();

        // Retrieve accepted return values recorded before the end
        // of the reporting period.
        var returns = (
            from returnLine in _dbContext.ConsignmentReturnLines.AsNoTracking()
            join returnHeader in _dbContext.ConsignmentReturns.AsNoTracking()
                on returnLine.ConsignmentReturnId equals returnHeader.Id
            where consignmentIds.Contains(returnHeader.ConsignmentId)
                  && returnHeader.ReturnDate < endUtc
            select new
            {
                returnHeader.ConsignmentId,
                AcceptedValue =
                    (decimal)returnLine.QuantityAccepted * returnLine.UnitPrice
            })
            .ToList();

        var returnTotals = returns
            .GroupBy(r => r.ConsignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(r => r.AcceptedValue));

        // Retrieve payments recorded before the end of the period.
        var payments = _dbContext.ConsignmentPayments
            .AsNoTracking()
            .Where(p =>
                consignmentIds.Contains(p.ConsignmentId) &&
                p.PaymentDate < endUtc)
            .Select(p => new
            {
                p.ConsignmentId,
                p.Amount
            })
            .ToList();

        var paymentTotals = payments
            .GroupBy(p => p.ConsignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(p => p.Amount));

        // Build the statement rows.
        var rows = consignments
            .Select(consignment =>
            {
                returnTotals.TryGetValue(
                    consignment.Id, out decimal acceptedReturns);

                paymentTotals.TryGetValue(
                    consignment.Id, out decimal totalPayments);

                return new ConsignmentStatementRow
                {
                    ConsignmentId = consignment.Id,
                    IssueDate = consignment.IssuedAt.ToLocalTime(),
                    ReferenceNumber = consignment.ReferenceNumber,
                    OriginalGoodsValue = consignment.OriginalGoodsValue,
                    AcceptedReturnsValue = acceptedReturns,
                    ConsignmentFee = consignment.ConsignmentFee,
                    Payments = totalPayments
                };
            })
            .Where(row =>
                row.IssueDate.Date >= startDate.Date ||
                row.FinalBalance > 0)
            .ToList();

        return new ConsignmentStatementResult
        {
            CustomerId = customerId,
            CustomerName = customerName,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Transactions = rows
        };
    }

    public InventoryReportResult GetInventoryReport()
    {
        // Retrieve all products.
        var products = _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToList();

        // Retrieve original consignment quantities.
        var consignmentLines = _dbContext.ConsignmentLines
            .AsNoTracking()
            .Select(line => new
            {
                line.Id,
                line.ProductId,
                line.Quantity
            })
            .ToList();

        // Retrieve quantities accepted from consignment returns.
        var acceptedReturns = _dbContext.ConsignmentReturnLines
            .AsNoTracking()
            .GroupBy(line => line.ConsignmentLineId)
            .Select(group => new
            {
                ConsignmentLineId = group.Key,
                AcceptedQuantity = group.Sum(line => line.QuantityAccepted)
            })
            .ToDictionary(
                item => item.ConsignmentLineId,
                item => item.AcceptedQuantity);

        // Calculate outstanding quantities per product.
        var outstandingQuantities = consignmentLines
            .GroupBy(line => line.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line =>
                {
                    acceptedReturns.TryGetValue(
                        line.Id, out int acceptedQuantity);

                    return Math.Max(
                        0, line.Quantity - acceptedQuantity);
                }));

        var rows = products.Select(product =>
        {
            outstandingQuantities.TryGetValue(
                product.Id, out int outstandingQuantity);

            return new InventoryReportRow
            {
                ProductId = product.Id,
                ProductName = product.Name,
                AvailableStock = product.StockQuantity,
                OutstandingConsignmentQuantity = outstandingQuantity,
                MinimumStockLevel = product.MinimumStockLevel
            };
        }).ToList();

        return new InventoryReportResult
        {
            GeneratedAt = DateTime.Now,
            Products = rows
        };
    }
}
