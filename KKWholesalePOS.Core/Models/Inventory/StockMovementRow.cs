namespace KKWholesalePOS.Core.Models.Inventory;

public class StockMovementRow
{
    public int MovementId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int QuantityChange { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
}