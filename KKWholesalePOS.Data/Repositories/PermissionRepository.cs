using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _dbContext;

    public PermissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Permission? GetById(int permissionId)
    {
        return _dbContext.Permissions
            .FirstOrDefault(p => p.Id == permissionId);
    }

    public Permission? GetByName(string name)
    {
        return _dbContext.Permissions
            .FirstOrDefault(p => p.Name == name);
    }

    public List<Permission> GetAll()
    {
        return _dbContext.Permissions
            .OrderBy(p => p.Name)
            .ToList();
    }

    public void Add(Permission permission)
    {
        _dbContext.Permissions.Add(permission);
    }

    public void Update(Permission permission)
    {
        _dbContext.Permissions.Update(permission);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }
}