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
        Transactions.Sum(transaction => transaction.OriginalLineValue);

    public decimal TotalAcceptedReturns =>
        Transactions.Sum(transaction => transaction.AcceptedReturnsValue);

    public decimal TotalFees { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal TotalOutstanding =>
        TotalOriginalValue - TotalAcceptedReturns + TotalFees - TotalPayments;
}