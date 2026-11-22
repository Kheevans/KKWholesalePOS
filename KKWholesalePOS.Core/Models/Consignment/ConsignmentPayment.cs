
namespace KKWholesalePOS.Core.Models.Consignment;

public class ConsignmentPayment
{
    public int Id { get; set; }

    public int ConsignmentId { get; set; }

    public int UserId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = "Cash";

    public string? ReferenceNumber { get; set; }
}
