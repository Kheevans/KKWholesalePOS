namespace KKWholesalePOS.Core.Models.Reports;

public class CreditReportRow
{
    public int CreditSaleId { get; set; }

    public DateTime IssuedDate { get; set; }

    public DateTime DueDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal PurchaseAmount { get; set; }

    public decimal CreditFee { get; set; }

    public decimal TotalAmount => PurchaseAmount + CreditFee;

    public decimal AmountPaid { get; set; }

    public decimal OutstandingBalance => Math.Max(0m, TotalAmount - AmountPaid);

    public string Status =>
        OutstandingBalance <= 0m
            ? "Paid"
            : (DateTime.Now.Date > DueDate.Date ? "Overdue" : "Open");
}