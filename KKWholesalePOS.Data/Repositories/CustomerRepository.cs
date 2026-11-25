using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ??
            throw new ArgumentNullException(nameof(dbContext));
    }

    public List<Customer> GetAll()
    {
        return _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToList();
    }

    public Customer? GetById(int customerId)
    {
        return _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefault(c => c.Id == customerId);
    }

    public List<Customer> Search(string? searchTerm, bool includeInactive = false)
    {
        var query = _dbContext.Customers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(term)) ||
                (c.Address != null && c.Address.ToLower().Contains(term)));
        }

        return query
            .OrderBy(c => c.Name)
            .ToList();
    }

    public void Add(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _dbContext.Customers.Add(customer);
    }

    public void Update(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _dbContext.Customers.Update(customer);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }
}