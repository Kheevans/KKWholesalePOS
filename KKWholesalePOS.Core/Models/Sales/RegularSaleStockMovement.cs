namespace KKWholesalePOS.Core.Models;

public class RegularSaleStockMovement
{
    public int Id { get; set; }

    public int RegularSaleId { get; set; }

    public int ProductId { get; set; }

    public int QuantityChange { get; set; }

    public string MovementType { get; set; } = "Regular sale";

    public int CashierId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
