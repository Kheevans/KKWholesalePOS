using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Interfaces;

public interface ICustomerRepository
{
    List<Customer> GetAll();
}