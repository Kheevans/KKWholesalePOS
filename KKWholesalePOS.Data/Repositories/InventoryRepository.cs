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
        string normalized = barcode.Trim();

        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Barcode == normalized);
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
        var product = _dbContext.Products.FirstOrDefault(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");

        product.MinimumStockLevel = minimumStockLevel;
        _dbContext.SaveChanges();
    }

    public int AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        string normalizedName = product.Name.Trim();
        string normalizedBarcode = product.Barcode.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException("Product name is required.", nameof(product));

        if (string.IsNullOrWhiteSpace(normalizedBarcode))
            throw new ArgumentException("Barcode is required.", nameof(product));

        if (product.SellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative.", nameof(product));

        if (product.StockQuantity < 0)
            throw new ArgumentException("Initial stock quantity cannot be negative.", nameof(product));

        if (product.UnitsPerCase <= 0)
            throw new ArgumentException("Units per case must be greater than zero.", nameof(product));

        if (product.MinimumStockLevel < 0)
            throw new ArgumentException("Minimum stock level cannot be negative.", nameof(product));

        bool duplicateBarcode = _dbContext.Products
            .Any(p => p.Barcode == normalizedBarcode);

        if (duplicateBarcode)
            throw new InvalidOperationException($"A product with barcode '{normalizedBarcode}' already exists.");

        int initialStock = product.StockQuantity;

        var entity = new Product
        {
            Name = normalizedName,
            Barcode = normalizedBarcode,
            SellingPrice = product.SellingPrice,
            StockQuantity = initialStock > 0 ? 0 : initialStock,
            UnitsPerCase = product.UnitsPerCase,
            MinimumStockLevel = product.MinimumStockLevel
        };

        using var transaction = initialStock > 0
            ? _dbContext.Database.BeginTransaction()
            : null;

        try
        {
            _dbContext.Products.Add(entity);
            _dbContext.SaveChanges();

            if (initialStock > 0)
            {
                int actorUserId = _dbContext.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Id)
                    .Select(u => u.Id)
                    .FirstOrDefault();

                if (actorUserId > 0)
                {
                    _inventoryService.ApplyStockChange(
                        entity.Id,
                        initialStock,
                        actorUserId,
                        InventoryService.StockReceived,
                        "Initial product stock");
                    _dbContext.SaveChanges();
                }
                else
                {
                    entity.StockQuantity = initialStock;
                    _dbContext.SaveChanges();
                }
            }

            transaction?.Commit();
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }

        return entity.Id;
    }

    public void UpdateProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.Id <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(product));

        string normalizedName = product.Name.Trim();
        string normalizedBarcode = product.Barcode.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException("Product name is required.", nameof(product));

        if (string.IsNullOrWhiteSpace(normalizedBarcode))
            throw new ArgumentException("Barcode is required.", nameof(product));

        if (product.SellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative.", nameof(product));

        if (product.UnitsPerCase <= 0)
            throw new ArgumentException("Units per case must be greater than zero.", nameof(product));

        if (product.MinimumStockLevel < 0)
            throw new ArgumentException("Minimum stock level cannot be negative.", nameof(product));

        var existing = _dbContext.Products.FirstOrDefault(p => p.Id == product.Id)
            ?? throw new InvalidOperationException("Product not found.");

        bool duplicateBarcode = _dbContext.Products
            .Any(p => p.Id != product.Id && p.Barcode == normalizedBarcode);

        if (duplicateBarcode)
            throw new InvalidOperationException($"Another product already uses barcode '{normalizedBarcode}'.");

        existing.Name = normalizedName;
        existing.Barcode = normalizedBarcode;
        existing.SellingPrice = product.SellingPrice;
        existing.UnitsPerCase = product.UnitsPerCase;
        existing.MinimumStockLevel = product.MinimumStockLevel;

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