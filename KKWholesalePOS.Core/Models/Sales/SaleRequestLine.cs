namespace KKWholesalePOS.Core.Models.Sales;

public class SaleRequestLine
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public SaleRequestLine(int productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}
