namespace KKWholesalePOS.Core.Models.Inventory;

public class StockMovement
{
    public int Id { get; set; }

    // Transaction references
    public int? RegularSaleId { get; set; }

    public int? ConsignmentId { get; set; }

    public int? ConsignmentReturnId { get; set; }

    // Product and stock change
    public int ProductId { get; set; }

    // Negative = stock deduction; positive = stock addition
    public int QuantityChange { get; set; }

    // StockReceived, StockAdjustment, RegularSale,
    // ConsignmentIssued, ConsignmentReturn
    public string MovementType { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int UserId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}