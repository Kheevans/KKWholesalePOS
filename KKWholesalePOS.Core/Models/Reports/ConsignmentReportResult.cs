namespace KKWholesalePOS.Core.Models.Reports;

public class ConsignmentReportResult
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IReadOnlyList<ConsignmentReportRow> Consignments { get; set; }
        = Array.Empty<ConsignmentReportRow>();

    public decimal TotalOriginalValue => Consignments.Sum(c => c.OriginalGoodsValue);

    public decimal TotalAcceptedReturns => Consignments.Sum(c => c.AcceptedReturnsValue);

    public decimal TotalFees => Consignments.Sum(c => c.ConsignmentFee);

    public decimal TotalPayments => Consignments.Sum(c => c.Payments);

    public decimal TotalOutstanding => Consignments.Sum(c => c.OutstandingBalance);
}