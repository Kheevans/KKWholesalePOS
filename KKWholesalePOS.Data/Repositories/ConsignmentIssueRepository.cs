using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Data.Services;

namespace KKWholesalePOS.Data.Repositories;

public class ConsignmentIssueRepository : IConsignmentIssueRepository
{
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;

    public ConsignmentIssueRepository(
        AppDbContext context,
        InventoryService inventoryService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    public Product? GetProduct(int productId)
    {
        return _context.Products.Find(productId);
    }

    public async Task SaveConsignmentAsync(Consignment consignment)
    {
        ArgumentNullException.ThrowIfNull(consignment);

        if (consignment.UserId <= 0)
            throw new ArgumentException("Invalid user ID.", nameof(consignment));

        if (consignment.Lines == null || consignment.Lines.Count == 0)
            throw new InvalidOperationException("A consignment must contain at least one line item.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Consignments.Add(consignment);
            await _context.SaveChangesAsync();

            foreach (var line in consignment.Lines)
            {
                _inventoryService.ApplyStockChange(
                    productId: line.ProductId,
                    quantityChange: -line.Quantity,
                    userId: consignment.UserId,
                    movementType: InventoryService.ConsignmentIssued,
                    notes: $"Consignment Issued: {consignment.ReferenceNumber}",
                    consignmentId: consignment.Id);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            throw;
        }
    }
}