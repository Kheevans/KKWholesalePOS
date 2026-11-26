using KKWholesalePOS.Core.Models.Consignment;
using KKWholesalePOS.Core.Models.Inventory;

namespace KKWholesalePOS.Core.Interfaces;

public interface IConsignmentIssueRepository
{
    Product? GetProduct(int productId);
    Task SaveConsignmentAsync(Consignment consignment);
}