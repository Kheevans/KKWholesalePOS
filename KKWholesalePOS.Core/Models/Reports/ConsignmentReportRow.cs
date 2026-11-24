namespace KKWholesalePOS.Core.Models.Reports;

public class ConsignmentReportRow
{
    public int ConsignmentId { get; set; }

    public DateTime IssueDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal OriginalGoodsValue { get; set; }

    public decimal AcceptedReturnsValue { get; set; }

    public decimal ConsignmentFee { get; set; }

    public decimal Payments { get; set; }

    public decimal OutstandingBalance =>
        Math.Max(0m, OriginalGoodsValue - AcceptedReturnsValue + ConsignmentFee - Payments);

    public string Status => OutstandingBalance <= 0m ? "Settled" : "Open";
}