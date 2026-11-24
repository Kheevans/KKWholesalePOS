namespace KKWholesalePOS.Core.Models.Reports;

public class CreditStatementRow
{
    public int CreditSaleId { get; set; }

    public int CreditSaleLineId { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime DueDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}