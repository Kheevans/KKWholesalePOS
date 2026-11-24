using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;
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

    public IReadOnlyList<ConsignmentReturn> GetReturns(int consignmentId)
    {
        return _dbContext.ConsignmentReturns
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.ConsignmentId == consignmentId)
            .ToList();
    }

    public IReadOnlyList<ConsignmentPayment> GetPayments(int consignmentId)
    {
        return _dbContext.ConsignmentPayments
            .AsNoTracking()
            .Where(p => p.ConsignmentId == consignmentId)
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
        _dbContext.ConsignmentPayments.Add(payment);
        _dbContext.SaveChanges();
        return payment;
    }
}