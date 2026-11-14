using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Interfaces;

public interface IUserRepository
{
    User? GetByUsername(string username);

    User? GetById(int userId);

    bool HasAnyUsers();

    void Add(User user);

    void Update(User user);

    void SaveChanges();

    void AssignRole(int userId, int roleId);

    void RemoveRole(int userId, int roleId);

    List<Role> GetUserRoles(int userId);
}
