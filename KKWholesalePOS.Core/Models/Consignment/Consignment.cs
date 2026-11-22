namespace KKWholesalePOS.Core.Models.Consignment;

public class Consignment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public string ReferenceNumber { get; set; } = string.Empty;

    public decimal OriginalGoodsValue { get; set; }

    public decimal ConsignmentFee { get; set; }

    public decimal TotalAmount => OriginalGoodsValue + ConsignmentFee;

    public List<ConsignmentLine> Lines { get; set; } = new();
}
