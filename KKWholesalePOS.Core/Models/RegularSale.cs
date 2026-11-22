
namespace KKWholesalePOS.Core.Models;

public class RegularSale
{
    public int Id { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int CashierId { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public decimal CashReceived { get; set; }

    public decimal ChangeDue { get; set; }

    // "Cash" or "Card"
    public string PaymentMethod { get; set; } = "Cash";

    // Card payments only. The full card number is never stored.
    public string? CardType { get; set; }

    public string? CardLastFour { get; set; }

    public string? ApprovalCode { get; set; }

    public List<RegularSaleLine> Lines { get; set; } = new();
}
