using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Sales;

namespace KKWholesalePOS.Core.Interfaces;

public interface IRegularSaleRepository
{
    Product? FindProduct(string barcode);

    Product? FindProductById(int productId);

    RegularSale SaveCashSale(
        IReadOnlyList<SaleRequestLine> items,
        decimal cashReceived,
        int cashierId,
        string cashierName);

    RegularSale SaveCardSale(
        IReadOnlyList<SaleRequestLine> items,
        string cardType,
        string cardLastFour,
        string approvalCode,
        int cashierId,
        string cashierName);
}
