using KKWholesalePOS.Core.Models.Inventory;

namespace KKWholesalePOS.Core.Interfaces;

public interface IInventoryRepository
{
    List<Product> GetAllProducts();

    Product? GetProductById(int productId);

    Product? GetProductByBarcode(string barcode);

    List<Product> SearchProducts(string query);

    List<Product> GetLowStockProducts();

    List<Product> GetNegativeStockProducts();

    List<InventoryRow> GetInventory(string searchTerm);

    List<StockMovement> GetStockMovements(int? productId = null);

    List<StockMovementRow> GetStockMovementHistory(int productId);

    void UpdateMinimumStockLevel(int productId, int minimumStockLevel);

    int AddProduct(Product product);

    void UpdateProduct(Product product);

    Task ReceiveStockAsync(
        int productId,
        int quantity,
        int userId,
        string? notes = null);

    Task AdjustStockAsync(
        int productId,
        int quantityChange,
        int userId,
        string notes);
}