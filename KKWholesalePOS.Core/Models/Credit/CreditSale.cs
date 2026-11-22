namespace KKWholesalePOS.Core.Models.Credit;

public class CreditSale
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    public decimal PurchaseAmount { get; set; }

    public decimal CreditFee { get; set; }

    public decimal TotalAmount =>
        PurchaseAmount + CreditFee;

    public string ReferenceNumber { get; set; } = string.Empty;

    public List<CreditSaleLine> Lines { get; set; } = new();
}
