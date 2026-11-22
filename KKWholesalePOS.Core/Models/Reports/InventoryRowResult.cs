namespace KKWholesalePOS.Core.Models.Reports;

public class InventoryReportResult
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public IReadOnlyList<InventoryReportRow> Products { get; set; }
        = Array.Empty<InventoryReportRow>();

    public int TotalProducts => Products.Count;

    public int LowStockProductCount =>
        Products.Count(product => product.IsLowStock);
}