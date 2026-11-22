namespace KKWholesalePOS.Core.Models.Reports;

public class InventoryReportRow
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int AvailableStock { get; set; }

    public int OutstandingConsignmentQuantity { get; set; }

    public int MinimumStockLevel { get; set; }

    public bool IsLowStock =>
        AvailableStock <= MinimumStockLevel;
}