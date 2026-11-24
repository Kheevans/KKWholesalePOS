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
        Transactions.Sum(transaction => transaction.LineTotal);

    public decimal TotalFees { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal TotalOutstanding =>
        TotalPurchases + TotalFees - TotalPayments;
}