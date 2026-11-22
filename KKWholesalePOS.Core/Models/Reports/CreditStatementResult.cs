namespace KKWholesalePOS.Core.Models.Reports;

public class CreditStatementResult
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IReadOnlyList<CreditStatementRow> Transactions { get; set; }
        = Array.Empty<CreditStatementRow>();

    public decimal TotalPurchases =>
        Transactions.Sum(transaction => transaction.PurchaseAmount);

    public decimal TotalFees =>
        Transactions.Sum(transaction => transaction.CreditFee);

    public decimal TotalPayments =>
        Transactions.Sum(transaction => transaction.Payments);

    public decimal TotalOutstanding =>
        Transactions.Sum(transaction => transaction.OutstandingBalance);
}