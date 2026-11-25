namespace KKWholesalePOS.Core.Models.Reports;
public class InventoryReportResult
{
    public DateTime GeneratedAt { get; set; }
    public int TotalProducts { get; set; }
    public long TotalUnitsInStock { get; set; }
    public long TotalUnitsOnConsignment { get; set; }
    public int LowStockProductCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<InventoryReportRow> Products { get; set; } = new();
}