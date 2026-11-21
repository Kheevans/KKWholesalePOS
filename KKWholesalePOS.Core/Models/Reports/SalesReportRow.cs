namespace KKWholesalePOS.Core.Models.Reports;

public class SalesReportRow
{
    public int SaleId { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
}
