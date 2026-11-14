using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Interfaces;

public interface IPermissionRepository
{
    Permission? GetById(int permissionId);

    Permission? GetByName(string name);

    List<Permission> GetAll();

    void Add(Permission permission);

    void Update(Permission permission);

    void SaveChanges();
}