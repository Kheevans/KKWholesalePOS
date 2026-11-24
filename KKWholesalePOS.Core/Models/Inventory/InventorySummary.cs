namespace KKWholesalePOS.Core.Models.Inventory;

// Provides summary information for the inventory overview.
public class InventorySummary
{
    public int TotalProducts { get; set; }

    public int LowStockProductCount { get; set; }

    public int OutOfStockProductCount { get; set; }

    public int InvalidBalanceProductCount { get; set; }

    public long TotalAvailableUnits { get; set; }

    public long TotalOutstandingUnits { get; set; }

    // Calculate product counts and stock totals for the inventory overview
    public static InventorySummary FromRows(
        IEnumerable<InventoryRow> rows)
    {
        var products = rows.ToList();

        return new InventorySummary
        {
            TotalProducts = products.Count,

            LowStockProductCount = products.Count(
                product => product.IsLowStock),

            OutOfStockProductCount = products.Count(
                product => product.IsOutOfStock),

            InvalidBalanceProductCount = products.Count(
                product => product.HasInvalidBalance),

            TotalAvailableUnits = products.Sum(
                product => (long)product.AvailableStock),

            TotalOutstandingUnits = products.Sum(
                product => (long)product.OutstandingConsignmentQuantity)
        };
    }
}