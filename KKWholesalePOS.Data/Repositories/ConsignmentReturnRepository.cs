using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Services;
using KKWholesalePOS.Data.Services;
using Microsoft.EntityFrameworkCore;
using Consignment = KKWholesalePOS.Core.Models.Consignment.Consignment;

namespace KKWholesalePOS.Data.Repositories;

public class ConsignmentReturnRepository : IConsignmentReturnRepository
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryService _inventoryService;

    public ConsignmentReturnRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _inventoryService = new InventoryService(dbContext);
    }

    public Consignment? FindConsignmentByReference(string referenceNumber)
    {
        return _dbContext.Consignments
            .AsNoTracking()
            .Include(c => c.Lines)
            .FirstOrDefault(c => c.ReferenceNumber == referenceNumber);
    }

    public IReadOnlyList<Consignment> GetConsignmentsByCustomerId(int customerId)
    {
        return _dbContext.Consignments
            .AsNoTracking()
            .Include(c => c.Lines)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.IssuedAt)
            .ToList();
    }

    public IReadOnlyList<ConsignmentReturn> GetReturns(int consignmentId)
    {
        return _dbContext.ConsignmentReturns
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.ConsignmentId == consignmentId)
            .OrderByDescending(r => r.ReturnDate)
            .ToList();
    }

    public IReadOnlyList<ConsignmentPayment> GetPayments(int consignmentId)
    {
        return _dbContext.ConsignmentPayments
            .AsNoTracking()
            .Where(p => p.ConsignmentId == consignmentId)
            .OrderByDescending(p => p.PaymentDate)
            .ToList();
    }

    public ConsignmentReturn SaveReturn(ConsignmentReturn consignmentReturn)
    {
        using var transaction = _dbContext.Database.BeginTransaction();

        try
        {
            _dbContext.ConsignmentReturns.Add(consignmentReturn);
            _dbContext.SaveChanges(); // gives the return an Id

            foreach (var line in consignmentReturn.Lines
                         .Where(l => l.QuantityAccepted > 0))
            {
                int productId = _dbContext.ConsignmentLines
                    .AsNoTracking()
                    .Where(l => l.Id == line.ConsignmentLineId)
                    .Select(l => l.ProductId)
                    .Single();

                // Stock changes only go through InventoryService
                _inventoryService.ApplyStockChange(
                    productId: productId,
                    quantityChange: line.QuantityAccepted,
                    userId: consignmentReturn.UserId,
                    movementType: InventoryService.ConsignmentReturn,
                    consignmentReturnId: consignmentReturn.Id);
            }

            _dbContext.SaveChanges();
            transaction.Commit();

            return consignmentReturn;
        }
        catch
        {
            transaction.Rollback();
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    public ConsignmentPayment SavePayment(ConsignmentPayment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        decimal amount = Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero);

        if (amount <= 0m)
            throw new ArgumentException("Payment must be greater than zero.", nameof(payment));

        using var transaction = _dbContext.Database.BeginTransaction();

        try
        {
            var consignment = _dbContext.Consignments
                .AsNoTracking()
                .Include(c => c.Lines)
                .FirstOrDefault(c => c.Id == payment.ConsignmentId)
                ?? throw new InvalidOperationException("Consignment not found.");

            decimal acceptedReturnsValue = _dbContext.ConsignmentReturns
                .AsNoTracking()
                .Where(r => r.ConsignmentId == payment.ConsignmentId)
                .SelectMany(r => r.Lines)
                .Sum(l => l.QuantityAccepted * l.UnitPrice);

            decimal totalPaid = _dbContext.ConsignmentPayments
                .AsNoTracking()
                .Where(p => p.ConsignmentId == payment.ConsignmentId)
                .Sum(p => p.Amount);

            var settlement = ConsignmentSettlementCalculator.Calculate(
                consignment.OriginalGoodsValue,
                acceptedReturnsValue,
                totalPaid);

            if (settlement.IsSettled || amount > settlement.Outstanding)
            {
                throw new InvalidOperationException(
                    $"Payment exceeds the amount owed ({settlement.Outstanding:N2}).");
            }

            payment.Amount = amount;
            _dbContext.ConsignmentPayments.Add(payment);
            _dbContext.SaveChanges();
            transaction.Commit();

            return payment;
        }
        catch
        {
            transaction.Rollback();
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}