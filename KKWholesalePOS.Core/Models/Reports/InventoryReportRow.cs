namespace KKWholesalePOS.Core.Models.Reports;

public class InventoryReportRow
{
    public int ProductId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int UnitsPerCase { get; set; }

    public int AvailableStock { get; set; }

    public int OutstandingConsignmentQuantity { get; set; }

    public int MinimumStockLevel { get; set; }

    public decimal UnitSellingPrice { get; set; }

    public decimal StockRetailValue => AvailableStock * UnitSellingPrice;

    public bool IsLowStock => AvailableStock <= MinimumStockLevel;

    public string StockStatus =>
        AvailableStock == 0
            ? "Out of Stock"
            : (AvailableStock <= MinimumStockLevel ? "Low Stock" : "In Stock");
}