namespace KKWholesalePOS.Core.Models;

public class StockMovement
{
    public int Id { get; set; }

    public int? RegularSaleId { get; set; }

    public int ProductId { get; set; }

    public int QuantityChange { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}