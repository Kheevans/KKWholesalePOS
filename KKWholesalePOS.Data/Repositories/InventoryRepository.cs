using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryService _inventoryService;

    public InventoryRepository(
        AppDbContext dbContext,
        InventoryService inventoryService)
    {
        _dbContext = dbContext ??
            throw new ArgumentNullException(nameof(dbContext));
        _inventoryService = inventoryService ??
            throw new ArgumentNullException(nameof(inventoryService));
    }

    public List<Product> GetAllProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToList();
    }

    public Product? GetProductById(int productId)
    {
        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Id == productId);
    }

    public Product? GetProductByBarcode(string barcode)
    {
        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Barcode == barcode);
    }

    public List<Product> SearchProducts(string query)
    {
        string term = query.Trim().ToLower();

        return _dbContext.Products
            .AsNoTracking()
            .Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Barcode.ToLower().Contains(term))
            .OrderBy(p => p.Name)
            .ToList();
    }

    public List<Product> GetLowStockProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= p.MinimumStockLevel)
            .OrderBy(p => p.Name)
            .ToList();
    }

    public List<Product> GetNegativeStockProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity < 0)
            .OrderBy(p => p.Name)
            .ToList();
    }

    // Retrieve products with their available and outstanding stock quantities
    public List<InventoryRow> GetInventory(string searchTerm)
    {
        var products = string.IsNullOrWhiteSpace(searchTerm)
            ? GetAllProducts()
            : SearchProducts(searchTerm);

        var consignmentLines = _dbContext.ConsignmentLines
            .AsNoTracking()
            .Select(line => new
            {
                line.Id,
                line.ProductId,
                line.Quantity
            })
            .ToList();

        // Calculate the total accepted returns for each consignment line
        var acceptedReturns = _dbContext.ConsignmentReturnLines
            .AsNoTracking()
            .GroupBy(line => line.ConsignmentLineId)
            .Select(group => new
            {
                ConsignmentLineId = group.Key,
                AcceptedQuantity = group.Sum(line => line.QuantityAccepted)
            })
            .ToDictionary(
                line => line.ConsignmentLineId,
                line => line.AcceptedQuantity);

        // Calculate outstanding quantities and identify invalid consignment balances
        var consignmentBalances = consignmentLines
            .Select(line =>
            {
                acceptedReturns.TryGetValue(line.Id, out int acceptedQuantity);

                return new
                {
                    line.ProductId,
                    OutstandingQuantity = line.Quantity - acceptedQuantity
                };
            })
            .GroupBy(line => line.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    OutstandingQuantity = group.Sum(line => line.OutstandingQuantity),
                    HasInvalidBalance = group.Any(line => line.OutstandingQuantity < 0)
                });

        return products.Select(product =>
        {
            consignmentBalances.TryGetValue(product.Id, out var balance);

            return new InventoryRow
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                SellingPrice = product.SellingPrice,
                UnitsPerCase = product.UnitsPerCase,
                AvailableStock = product.StockQuantity,
                OutstandingConsignmentQuantity = balance?.OutstandingQuantity ?? 0,
                MinimumStockLevel = product.MinimumStockLevel,
                HasInvalidConsignmentBalance = balance?.HasInvalidBalance ?? false
            };
        }).ToList();
    }

    public List<StockMovement> GetStockMovements(int? productId = null)
    {
        var query = _dbContext.StockMovements.AsNoTracking();

        if (productId.HasValue)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        return query
            .OrderByDescending(m => m.RecordedAt)
            .ThenByDescending(m => m.Id)
            .ToList();
    }

    // Retrieve stock movements with the associated product and username
    public List<StockMovementRow> GetStockMovementHistory(int productId)
    {
        return (
            from movement in _dbContext.StockMovements.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on movement.ProductId equals product.Id
            join user in _dbContext.Users.AsNoTracking()
                on movement.UserId equals user.Id
            where movement.ProductId == productId
            orderby movement.RecordedAt descending, movement.Id descending
            select new StockMovementRow
            {
                MovementId = movement.Id,
                ProductId = movement.ProductId,
                ProductName = product.Name,
                QuantityChange = movement.QuantityChange,
                MovementType = movement.MovementType,
                Notes = movement.Notes,
                UserId = movement.UserId,
                Username = user.Username,
                RecordedAt = movement.RecordedAt
            }).ToList();
    }

    public void UpdateMinimumStockLevel(int productId, int minimumStockLevel)
    {
        var product = _dbContext.Products.FirstOrDefault(p => p.Id == productId);

        if (product == null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        product.MinimumStockLevel = minimumStockLevel;
        _dbContext.SaveChanges();
    }

    public Task ReceiveStockAsync(
        int productId,
        int quantity,
        int userId,
        string? notes = null)
    {
        return _inventoryService.ReceiveStockAsync(
            productId,
            quantity,
            userId,
            notes);
    }

    public Task AdjustStockAsync(
        int productId,
        int quantityChange,
        int userId,
        string notes)
    {
        return _inventoryService.AdjustStockAsync(
            productId,
            quantityChange,
            userId,
            notes);
    }
}