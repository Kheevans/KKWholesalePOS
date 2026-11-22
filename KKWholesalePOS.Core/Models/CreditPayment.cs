
namespace KKWholesalePOS.Core.Models;

public class CreditPayment
{
    public int Id { get; set; }

    public int CreditSaleId { get; set; }

    public int UserId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = "Cash";

    public string? ReferenceNumber { get; set; }
}
