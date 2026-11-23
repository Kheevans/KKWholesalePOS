using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Interfaces;

public interface IRoleRepository
{
    Role? GetById(int roleId);

    Role? GetByName(string name);

    List<Role> GetAll();

    void Add(Role role);

    void Update(Role role);

    void SaveChanges();
}