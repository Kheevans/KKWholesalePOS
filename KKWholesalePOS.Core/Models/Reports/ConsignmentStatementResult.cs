namespace KKWholesalePOS.Core.Models.Reports;

public class ConsignmentStatementResult
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IReadOnlyList<ConsignmentStatementRow> Transactions { get; set; }
        = Array.Empty<ConsignmentStatementRow>();

    public decimal TotalOriginalValue =>
        Transactions.Sum(transaction => transaction.OriginalGoodsValue);

    public decimal TotalAcceptedReturns =>
        Transactions.Sum(transaction => transaction.AcceptedReturnsValue);

    public decimal TotalFees =>
        Transactions.Sum(transaction => transaction.ConsignmentFee);

    public decimal TotalPayments =>
        Transactions.Sum(transaction => transaction.Payments);

    public decimal TotalOutstanding =>
        Transactions.Sum(transaction => transaction.FinalBalance);
}