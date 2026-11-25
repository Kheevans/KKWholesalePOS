using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Interfaces;

public interface ICustomerRepository
{
    List<Customer> GetAll();
    Customer? GetById(int customerId);
    List<Customer> Search(string? searchTerm, bool includeInactive = false);
    void Add(Customer customer);
    void Update(Customer customer);
    void SaveChanges();
}