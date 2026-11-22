using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Interfaces;

public interface ICustomerRepository
{
    List<Customer> GetAll();
}