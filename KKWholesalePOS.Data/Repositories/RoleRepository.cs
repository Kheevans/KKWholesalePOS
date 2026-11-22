using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Role? GetById(int roleId)
    {
        return _dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(r => r.Id == roleId);
    }

    public Role? GetByName(string name)
    {
        return _dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(r => r.Name == name);
    }

    public List<Role> GetAll()
    {
        return _dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToList();
    }

    public void Add(Role role)
    {
        _dbContext.Roles.Add(role);
    }

    public void Update(Role role)
    {
        _dbContext.Roles.Update(role);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }
}