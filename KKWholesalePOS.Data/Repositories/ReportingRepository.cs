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

    public CreditReportResult GetCreditReport(
        DateTime startDate,
        DateTime endDate)
    {
        DateTime startLocal = DateTime.SpecifyKind(
            startDate.Date, DateTimeKind.Local);

        DateTime endLocalExclusive = DateTime.SpecifyKind(
            endDate.Date.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocalExclusive.ToUniversalTime();

        var customersById = _dbContext.Customers
            .AsNoTracking()
            .ToDictionary(c => c.Id, c => c.Name);

        var sales = _dbContext.CreditSales
            .AsNoTracking()
            .Where(s =>
                s.IssuedAt >= startUtc &&
                s.IssuedAt < endUtc)
            .OrderBy(s => s.IssuedAt)
            .Select(s => new
            {
                s.Id,
                s.CustomerId,
                s.IssuedAt,
                s.DueDate,
                s.ReferenceNumber,
                s.PurchaseAmount,
                s.CreditFee
            })
            .ToList();

        var saleIds = sales.Select(s => s.Id).ToList();

        var paymentTotals = saleIds.Count == 0
            ? new Dictionary<int, decimal>()
            : _dbContext.CreditPayments
                .AsNoTracking()
                .Where(p =>
                    saleIds.Contains(p.CreditSaleId) &&
                    p.PaymentDate < endUtc)
                .Select(p => new { p.CreditSaleId, p.Amount })
                .ToList()
                .GroupBy(p => p.CreditSaleId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var rows = sales.Select(sale =>
        {
            paymentTotals.TryGetValue(sale.Id, out decimal paid);
            customersById.TryGetValue(sale.CustomerId, out string? customerName);

            return new CreditReportRow
            {
                CreditSaleId = sale.Id,
                IssuedDate = sale.IssuedAt.ToLocalTime(),
                DueDate = sale.DueDate,
                ReferenceNumber = sale.ReferenceNumber,
                CustomerName = customerName ?? $"Customer #{sale.CustomerId}",
                PurchaseAmount = sale.PurchaseAmount,
                CreditFee = sale.CreditFee,
                AmountPaid = paid
            };
        }).ToList();

        return new CreditReportResult
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Accounts = rows
        };
    }

    public ConsignmentReportResult GetConsignmentReport(
        DateTime startDate,
        DateTime endDate)
    {
        DateTime startLocal = DateTime.SpecifyKind(
            startDate.Date, DateTimeKind.Local);

        DateTime endLocalExclusive = DateTime.SpecifyKind(
            endDate.Date.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocalExclusive.ToUniversalTime();

        var customersById = _dbContext.Customers
            .AsNoTracking()
            .ToDictionary(c => c.Id, c => c.Name);

        var consignments = _dbContext.Consignments
            .AsNoTracking()
            .Where(c =>
                c.IssuedAt >= startUtc &&
                c.IssuedAt < endUtc)
            .OrderBy(c => c.IssuedAt)
            .Select(c => new
            {
                c.Id,
                c.CustomerId,
                c.IssuedAt,
                c.ReferenceNumber,
                c.OriginalGoodsValue,
                c.ConsignmentFee
            })
            .ToList();

        var consignmentIds = consignments.Select(c => c.Id).ToList();

        var returnTotals = consignmentIds.Count == 0
            ? new Dictionary<int, decimal>()
            : (
                from returnLine in _dbContext.ConsignmentReturnLines.AsNoTracking()
                join returnHeader in _dbContext.ConsignmentReturns.AsNoTracking()
                    on returnLine.ConsignmentReturnId equals returnHeader.Id
                where consignmentIds.Contains(returnHeader.ConsignmentId)
                      && returnHeader.ReturnDate < endUtc
                select new
                {
                    returnHeader.ConsignmentId,
                    AcceptedValue = (decimal)returnLine.QuantityAccepted * returnLine.UnitPrice
                })
                .ToList()
                .GroupBy(r => r.ConsignmentId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AcceptedValue));

        var paymentTotals = consignmentIds.Count == 0
            ? new Dictionary<int, decimal>()
            : _dbContext.ConsignmentPayments
                .AsNoTracking()
                .Where(p =>
                    consignmentIds.Contains(p.ConsignmentId) &&
                    p.PaymentDate < endUtc)
                .Select(p => new { p.ConsignmentId, p.Amount })
                .ToList()
                .GroupBy(p => p.ConsignmentId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var rows = consignments.Select(consignment =>
        {
            returnTotals.TryGetValue(consignment.Id, out decimal acceptedReturns);
            paymentTotals.TryGetValue(consignment.Id, out decimal paid);
            customersById.TryGetValue(consignment.CustomerId, out string? customerName);

            return new ConsignmentReportRow
            {
                ConsignmentId = consignment.Id,
                IssueDate = consignment.IssuedAt.ToLocalTime(),
                ReferenceNumber = consignment.ReferenceNumber,
                CustomerName = customerName ?? $"Customer #{consignment.CustomerId}",
                OriginalGoodsValue = consignment.OriginalGoodsValue,
                AcceptedReturnsValue = acceptedReturns,
                ConsignmentFee = consignment.ConsignmentFee,
                Payments = paid
            };
        }).ToList();

        return new ConsignmentReportResult
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Consignments = rows
        };
    }

    public CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate,
        string? referenceNumber = null)
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

        var sales = _dbContext.CreditSales
            .AsNoTracking()
            .Include(s => s.Lines)
            .Where(s =>
                s.CustomerId == customerId &&
                s.IssuedAt < endUtc)
            .OrderBy(s => s.IssuedAt)
            .ThenBy(s => s.Id)
            .ToList();

        var saleIds = sales.Select(s => s.Id).ToList();

        var paymentsBySale = saleIds.Count == 0
            ? new Dictionary<int, decimal>()
            : _dbContext.CreditPayments
                .AsNoTracking()
                .Where(p =>
                    saleIds.Contains(p.CreditSaleId) &&
                    p.PaymentDate < endUtc)
                .Select(p => new { p.CreditSaleId, p.Amount })
                .ToList()
                .GroupBy(p => p.CreditSaleId)
                .ToDictionary(group => group.Key, group => group.Sum(p => p.Amount));

        var includedSales = sales
            .Where(sale =>
            {
                paymentsBySale.TryGetValue(sale.Id, out decimal paid);
                decimal balance = sale.PurchaseAmount + sale.CreditFee - paid;
                return sale.IssuedAt.ToLocalTime().Date >= startDate.Date || balance > 0m;
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(referenceNumber))
        {
            includedSales = includedSales
                .Where(s => s.ReferenceNumber == referenceNumber)
                .ToList();
        }

        var rows = includedSales
            .SelectMany(sale => sale.Lines
                .OrderBy(line => line.Id)
                .Select(line =>
                {
                    decimal total = line.LineTotal > 0m
                        ? line.LineTotal
                        : line.UnitPrice * line.Quantity;

                    return new CreditStatementRow
                    {
                        CreditSaleId = sale.Id,
                        CreditSaleLineId = line.Id,
                        TransactionDate = sale.IssuedAt.ToLocalTime(),
                        DueDate = sale.DueDate,
                        ReferenceNumber = sale.ReferenceNumber,
                        ProductName = line.ProductName,
                        Barcode = line.Barcode,
                        UnitPrice = line.UnitPrice,
                        Quantity = line.Quantity,
                        LineTotal = total
                    };
                }))
            .ToList();

        decimal totalFees = includedSales.Sum(s => s.CreditFee);
        decimal totalPayments = includedSales.Sum(s =>
            paymentsBySale.TryGetValue(s.Id, out decimal paid) ? paid : 0m);

        return new CreditStatementResult
        {
            CustomerId = customerId,
            CustomerName = customerName,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Transactions = rows,
            TotalFees = totalFees,
            TotalPayments = totalPayments
        };
    }

    public ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate,
        string? referenceNumber = null)
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

        var consignments = _dbContext.Consignments
            .AsNoTracking()
            .Include(c => c.Lines)
            .Where(c =>
                c.CustomerId == customerId &&
                c.IssuedAt < endUtc)
            .OrderBy(c => c.IssuedAt)
            .ThenBy(c => c.Id)
            .ToList();

        var consignmentIds = consignments
            .Select(c => c.Id)
            .ToList();

        var lineReturns = consignmentIds.Count == 0
            ? new Dictionary<int, (int QuantityAccepted, decimal AcceptedValue)>()
            : (
                from returnLine in _dbContext.ConsignmentReturnLines.AsNoTracking()
                join returnHeader in _dbContext.ConsignmentReturns.AsNoTracking()
                    on returnLine.ConsignmentReturnId equals returnHeader.Id
                where consignmentIds.Contains(returnHeader.ConsignmentId)
                      && returnHeader.ReturnDate < endUtc
                select new
                {
                    returnLine.ConsignmentLineId,
                    returnLine.QuantityAccepted,
                    AcceptedValue = (decimal)returnLine.QuantityAccepted * returnLine.UnitPrice
                })
                .ToList()
                .GroupBy(r => r.ConsignmentLineId)
                .ToDictionary(
                    group => group.Key,
                    group => (
                        QuantityAccepted: group.Sum(r => r.QuantityAccepted),
                        AcceptedValue: group.Sum(r => r.AcceptedValue)));

        var paymentsByConsignment = consignmentIds.Count == 0
            ? new Dictionary<int, decimal>()
            : _dbContext.ConsignmentPayments
                .AsNoTracking()
                .Where(p =>
                    consignmentIds.Contains(p.ConsignmentId) &&
                    p.PaymentDate < endUtc)
                .Select(p => new
                {
                    p.ConsignmentId,
                    p.Amount
                })
                .ToList()
                .GroupBy(p => p.ConsignmentId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(p => p.Amount));

        var includedConsignments = consignments
            .Where(consignment =>
            {
                decimal acceptedReturns = consignment.Lines.Sum(line =>
                    lineReturns.TryGetValue(line.Id, out var ret) ? ret.AcceptedValue : 0m);

                paymentsByConsignment.TryGetValue(consignment.Id, out decimal paid);

                decimal balance =
                    consignment.OriginalGoodsValue
                    - acceptedReturns
                    + consignment.ConsignmentFee
                    - paid;

                return consignment.IssuedAt.ToLocalTime().Date >= startDate.Date || balance > 0m;
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(referenceNumber))
        {
            includedConsignments = includedConsignments
                .Where(c => c.ReferenceNumber == referenceNumber)
                .ToList();
        }

        var rows = includedConsignments
            .SelectMany(consignment => consignment.Lines
                .OrderBy(line => line.Id)
                .Select(line =>
                {
                    lineReturns.TryGetValue(line.Id, out var ret);

                    decimal originalValue = line.LineTotal > 0m
                        ? line.LineTotal
                        : line.UnitPrice * line.Quantity;

                    return new ConsignmentStatementRow
                    {
                        ConsignmentId = consignment.Id,
                        ConsignmentLineId = line.Id,
                        IssueDate = consignment.IssuedAt.ToLocalTime(),
                        ReferenceNumber = consignment.ReferenceNumber,
                        ProductName = line.ProductName,
                        Barcode = line.Barcode,
                        UnitPrice = line.UnitPrice,
                        QuantityIssued = line.Quantity,
                        QuantityAcceptedReturned = ret.QuantityAccepted,
                        OriginalLineValue = originalValue,
                        AcceptedReturnsValue = ret.AcceptedValue
                    };
                }))
            .ToList();

        decimal totalFees = includedConsignments.Sum(c => c.ConsignmentFee);
        decimal totalPayments = includedConsignments.Sum(c =>
            paymentsByConsignment.TryGetValue(c.Id, out decimal paid) ? paid : 0m);

        return new ConsignmentStatementResult
        {
            CustomerId = customerId,
            CustomerName = customerName,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Transactions = rows,
            TotalFees = totalFees,
            TotalPayments = totalPayments
        };
    }

    public InventoryReportResult GetInventoryReport()
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToList();

        var consignmentLines = _dbContext.ConsignmentLines
            .AsNoTracking()
            .Select(line => new
            {
                line.Id,
                line.ProductId,
                line.Quantity
            })
            .ToList();

        var acceptedReturns = _dbContext.ConsignmentReturnLines
            .AsNoTracking()
            .Select(line => new
            {
                line.ConsignmentLineId,
                line.QuantityAccepted
            })
            .ToList()
            .GroupBy(line => line.ConsignmentLineId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line => line.QuantityAccepted));

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
                Barcode = product.Barcode,
                ProductName = product.Name,
                UnitsPerCase = product.UnitsPerCase,
                AvailableStock = product.StockQuantity,
                OutstandingConsignmentQuantity = outstandingQuantity,
                MinimumStockLevel = product.MinimumStockLevel,
                UnitSellingPrice = product.SellingPrice
            };
        }).ToList();

        return new InventoryReportResult
        {
            GeneratedAt = DateTime.Now,
            Products = rows
        };
    }
}