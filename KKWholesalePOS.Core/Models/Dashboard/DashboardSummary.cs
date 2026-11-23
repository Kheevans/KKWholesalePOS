
namespace KKWholesalePOS.Core.Models.Dashboard;

public class DashboardSummary
{
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }

    public decimal SalesToday { get; set; }
    public int TransactionsToday { get; set; }

    public int PendingConsignments { get; set; }
    public decimal OutstandingConsignmentBalance { get; set; }

    public decimal OutstandingCreditBalance { get; set; }
    public int OverdueCreditAccounts { get; set; }

    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
