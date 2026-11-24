namespace KKWholesalePOS.Core.Models;

// Products that do not attract GCT (for example basic food items).
// Every other product is charged GCT.
public class ZeroRatedProduct
{
    public int ProductId { get; set; }
}
