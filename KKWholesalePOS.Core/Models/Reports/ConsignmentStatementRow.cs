namespace KKWholesalePOS.Core.Models.Reports;

public class ConsignmentStatementRow
{
    public int ConsignmentId { get; set; }

    public DateTime IssueDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public decimal OriginalGoodsValue { get; set; }

    public decimal AcceptedReturnsValue { get; set; }

    public decimal ConsignmentFee { get; set; }

    public decimal Payments { get; set; }

    public decimal FinalBalance =>
        OriginalGoodsValue - AcceptedReturnsValue
        + ConsignmentFee - Payments;
}