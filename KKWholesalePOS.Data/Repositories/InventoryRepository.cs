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