
using KKWholesalePOS.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Services;

public class InventoryService
{
    private readonly AppDbContext _context;

    public const string StockReceived = "StockReceived";
    public const string StockAdjustment = "StockAdjustment";
    public const string RegularSale = "RegularSale";
    public const string ConsignmentIssued = "ConsignmentIssued";
    public const string ConsignmentReturn = "ConsignmentReturn";
    //newly add; core depends directly on data
    public const string CreditSale = "CreditSale";

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    // Standalone stock receiving operation
    public async Task ReceiveStockAsync(
        int productId,
        int quantity,
        int userId,
        string? notes = null)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "Received quantity must be greater than zero.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            ApplyStockChange(
                productId,
                quantity,
                userId,
                StockReceived,
                notes);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Standalone stock adjustment operation
    public async Task AdjustStockAsync(
        int productId,
        int quantityChange,
        int userId,
        string notes)
    {
        if (quantityChange == 0)
            throw new ArgumentException(
                "Adjustment quantity cannot be zero.");

        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException(
                "An adjustment reason is required.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            ApplyStockChange(
                productId,
                quantityChange,
                userId,
                StockAdjustment,
                notes);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    // Shared stock-changing operation.
    // The calling operation must manage the active transaction
    // and call SaveChanges() before committing.
    public void ApplyStockChange(
        int productId,
        int quantityChange,
        int userId,
        string movementType,
        string? notes = null,
        int? regularSaleId = null,
        int? consignmentId = null,
        int? consignmentReturnId = null)
    {
        if (productId <= 0)
            throw new ArgumentException("Invalid product ID.");

        if (quantityChange == 0)
            throw new ArgumentException(
                "Stock change cannot be zero.");

        if (userId <= 0)
            throw new ArgumentException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(movementType))
            throw new ArgumentException(
                "Movement type is required.");

        // Stock changes must participate in an existing transaction.
        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException(
                "Stock changes must be performed inside a database transaction.");

        int sourceCount =
            (regularSaleId.HasValue ? 1 : 0) +
            (consignmentId.HasValue ? 1 : 0) +
            (consignmentReturnId.HasValue ? 1 : 0);

        if (sourceCount > 1)
            throw new ArgumentException(
                "A stock movement can reference only one transaction.");

        // Validate movement type, source reference and stock direction.
        bool validMovement = movementType switch
        {
            StockReceived =>
                quantityChange > 0 && sourceCount == 0,

            StockAdjustment =>
                sourceCount == 0,

            RegularSale =>
                quantityChange < 0 &&
                regularSaleId.HasValue &&
                sourceCount == 1,

            ConsignmentIssued =>
                quantityChange < 0 &&
                consignmentId.HasValue &&
                sourceCount == 1,

            ConsignmentReturn =>
                quantityChange > 0 &&
                consignmentReturnId.HasValue &&
                sourceCount == 1,
            
            //newly added
            CreditSale => 
                quantityChange >0 &&
                sourceCount == 0,

            _ => false
        };

        if (!validMovement)
            throw new ArgumentException(
                "The movement type, quantity direction and transaction reference are inconsistent.");

        // Update stock directly in the database.
        // The condition prevents inventory from becoming negative.
        int affectedRows = _context.Database.ExecuteSqlInterpolated(
            $"UPDATE Products SET StockQuantity = StockQuantity + {quantityChange} WHERE Id = {productId} AND StockQuantity + {quantityChange} >= 0");

        if (affectedRows != 1)
        {
            bool productExists = _context.Products
                .Any(p => p.Id == productId);

            if (!productExists)
                throw new InvalidOperationException(
                    "Product not found.");

            throw new InvalidOperationException(
                "Insufficient stock for this operation.");
        }

        // The caller saves this movement with SaveChanges().
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            QuantityChange = quantityChange,
            MovementType = movementType,
            Notes = notes,
            UserId = userId,
            RegularSaleId = regularSaleId,
            ConsignmentId = consignmentId,
            ConsignmentReturnId = consignmentReturnId,
            RecordedAt = DateTime.UtcNow
        });
    }

}
