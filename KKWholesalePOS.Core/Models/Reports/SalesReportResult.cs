namespace KKWholesalePOS.Core.Models.Reports;

public class SalesReportResult
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IReadOnlyList<SalesReportRow> Transactions { get; set; }
        = Array.Empty<SalesReportRow>();

    public int TransactionCount => Transactions.Count;

    public decimal TotalSalesRevenue =>
        Transactions.Sum(transaction => transaction.TotalAmount);
}