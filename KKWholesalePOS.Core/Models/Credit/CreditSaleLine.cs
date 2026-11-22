namespace KKWholesalePOS.Core.Models.Credit;

public class CreditSaleLine
{
    public int Id { get; set; }

    public int CreditSaleId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
