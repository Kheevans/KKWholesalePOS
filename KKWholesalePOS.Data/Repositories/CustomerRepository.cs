using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<Customer> GetAll()
    {
        return _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToList();
    }
}