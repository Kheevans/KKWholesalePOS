namespace KKWholesalePOS.Core.Models.Reports;

public class CreditStatementRow
{
    public int CreditSaleId { get; set; }

    public DateTime TransactionDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public decimal PurchaseAmount { get; set; }

    public decimal CreditFee { get; set; }

    public decimal Payments { get; set; }

    public decimal OutstandingBalance =>
        PurchaseAmount + CreditFee - Payments;
}