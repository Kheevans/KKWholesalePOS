namespace KKWholesalePOS.Core.Models.Reports;

public class CreditReportResult
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public IReadOnlyList<CreditReportRow> Accounts { get; set; }
        = Array.Empty<CreditReportRow>();

    public decimal TotalPurchases => Accounts.Sum(a => a.PurchaseAmount);

    public decimal TotalFees => Accounts.Sum(a => a.CreditFee);

    public decimal TotalPaid => Accounts.Sum(a => a.AmountPaid);

    public decimal TotalOutstanding => Accounts.Sum(a => a.OutstandingBalance);
}