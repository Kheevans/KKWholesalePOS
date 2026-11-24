namespace KKWholesalePOS.Core.Models.Reports;

public class ConsignmentStatementRow
{
    public int ConsignmentId { get; set; }

    public int ConsignmentLineId { get; set; }

    public DateTime IssueDate { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int QuantityIssued { get; set; }

    public int QuantityAcceptedReturned { get; set; }

    public int RemainingQuantity =>
        Math.Max(0, QuantityIssued - QuantityAcceptedReturned);

    public decimal OriginalLineValue { get; set; }

    public decimal AcceptedReturnsValue { get; set; }

    public decimal NetGoodsValue =>
        OriginalLineValue - AcceptedReturnsValue;
}