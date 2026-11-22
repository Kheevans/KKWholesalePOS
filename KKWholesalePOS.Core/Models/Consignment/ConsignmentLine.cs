namespace KKWholesalePOS.Core.Models.Consignment;

public class ConsignmentLine
{
    public int Id { get; set; }

    public int ConsignmentId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
