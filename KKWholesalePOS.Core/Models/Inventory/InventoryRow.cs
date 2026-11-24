namespace KKWholesalePOS.Core.Models.Inventory;

public class InventoryRow
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public int UnitsPerCase { get; set; }

    public int AvailableStock { get; set; }

    public int OutstandingConsignmentQuantity { get; set; }

    public int MinimumStockLevel { get; set; }

    public bool HasInvalidConsignmentBalance { get; set; }

    // Identify products at or below the minimum stock level
    public bool IsLowStock =>
        AvailableStock <= MinimumStockLevel;

    public bool IsOutOfStock =>
        AvailableStock == 0;

    // Identify inconsistent inventory balances
    public bool HasInvalidBalance =>
        AvailableStock < 0 ||
        OutstandingConsignmentQuantity < 0 ||
        HasInvalidConsignmentBalance;

    // Determine the current inventory status
    public string Status
    {
        get
        {
            if (HasInvalidBalance)
                return "Invalid Balance";

            if (IsOutOfStock)
                return "Out of Stock";

            if (IsLowStock)
                return "Low Stock";

            return "In Stock";
        }
    }
}
