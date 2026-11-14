namespace KKWholesalePOS.Core.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public int StockQuantity { get; set; }

    public int UnitsPerCase { get; set; } = 1;
}