using KKWholesalePOS.Core;
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _dbContext;

    public DashboardRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ??
            throw new ArgumentNullException(nameof(dbContext));
    }

    public DashboardSummary GetDashboardSummary()
    {
        DateTime today = DateTime.Today;

        DateTime startLocal = DateTime.SpecifyKind(
            today, DateTimeKind.Local);

        DateTime endLocal = DateTime.SpecifyKind(
            today.AddDays(1), DateTimeKind.Local);

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocal.ToUniversalTime();

        var summary = new DashboardSummary
        {
            TotalProducts = _dbContext.Products.Count(),
            TotalCustomers = _dbContext.Customers.Count(),
            LowStockProducts = _dbContext.Products.Count(
                p => p.StockQuantity > 0 &&
                p.StockQuantity <= p.MinimumStockLevel),
            OutOfStockProducts = _dbContext.Products.Count(
                p => p.StockQuantity == 0)
        };

        var todaysSales = _dbContext.RegularSales
            .AsNoTracking()
            .Where(s =>
                s.CompletedAt >= startUtc &&
                s.CompletedAt < endUtc)
            .Select(s => new { s.Total })
            .ToList();

        summary.SalesToday = todaysSales.Sum(s => s.Total);
        summary.TransactionsToday = todaysSales.Count;

        var creditSales = _dbContext.CreditSales
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.IssuedAt,
                s.DueDate,
                s.PurchaseAmount,
                s.CreditFee
            })
            .ToList();

        var creditPayments = _dbContext.CreditPayments
            .AsNoTracking()
            .Select(p => new
            {
                p.CreditSaleId,
                p.Amount
            })
            .ToList();

        var creditPaymentTotals = creditPayments
            .GroupBy(p => p.CreditSaleId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(p => p.Amount));

        foreach (var sale in creditSales)
        {
            creditPaymentTotals.TryGetValue(sale.Id, out decimal payments);

            decimal balance = sale.PurchaseAmount + sale.CreditFee - payments;

            if (balance > 0)
            {
                summary.OutstandingCreditBalance += balance;

                DateTime dueDate = sale.DueDate != default
                    ? sale.DueDate.ToLocalTime().Date
                    : sale.IssuedAt.ToLocalTime().Date.AddDays(BusinessRules.CreditRepaymentDays);

                if (today > dueDate)
                {
                    summary.OverdueCreditAccounts++;
                }
            }
        }

        var consignments = _dbContext.Consignments
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.OriginalGoodsValue,
                c.ConsignmentFee
            })
            .ToList();

        var consignmentIds = consignments.Select(c => c.Id).ToList();

        var acceptedReturns = consignmentIds.Count == 0
            ? new[] { new { ConsignmentId = 0, AcceptedValue = 0m } }.Take(0).ToList()
            : (
                from line in _dbContext.ConsignmentReturnLines.AsNoTracking()
                join header in _dbContext.ConsignmentReturns.AsNoTracking()
                    on line.ConsignmentReturnId equals header.Id
                where consignmentIds.Contains(header.ConsignmentId)
                select new
                {
                    header.ConsignmentId,
                    AcceptedValue = (decimal)line.QuantityAccepted * line.UnitPrice
                }).ToList();

        var returnTotals = acceptedReturns
            .GroupBy(r => r.ConsignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(r => r.AcceptedValue));

        var consignmentPayments = consignmentIds.Count == 0
            ? new[] { new { ConsignmentId = 0, Amount = 0m } }.Take(0).ToList()
            : _dbContext.ConsignmentPayments
                .AsNoTracking()
                .Where(p => consignmentIds.Contains(p.ConsignmentId))
                .Select(p => new
                {
                    p.ConsignmentId,
                    p.Amount
                })
                .ToList();

        var paymentTotals = consignmentPayments
            .GroupBy(p => p.ConsignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(p => p.Amount));

        foreach (var consignment in consignments)
        {
            returnTotals.TryGetValue(consignment.Id, out decimal acceptedValue);
            paymentTotals.TryGetValue(consignment.Id, out decimal payments);

            decimal balance =
                consignment.OriginalGoodsValue
                - acceptedValue
                + consignment.ConsignmentFee
                - payments;

            if (balance > 0)
            {
                summary.PendingConsignments++;
                summary.OutstandingConsignmentBalance += balance;
            }
        }

        return summary;
    }
}