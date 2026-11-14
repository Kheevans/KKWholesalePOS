using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User? GetByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(u => u.Username == username);
    }

    public User? GetById(int userId)
    {
        return _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(u => u.Id == userId);
    }

    public bool HasAnyUsers()
    {
        return _dbContext.Users.Any();
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }

    public void AssignRole(int userId, int roleId)
    {
        var userRole = _dbContext.UserRoles
            .Find(userId, roleId);

        if (userRole == null)
        {
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            _dbContext.SaveChanges();
        }
    }

    public void RemoveRole(int userId, int roleId)
    {
        var userRole = _dbContext.UserRoles
            .Find(userId, roleId);

        if (userRole != null)
        {
            _dbContext.UserRoles.Remove(userRole);
            _dbContext.SaveChanges();
        }
    }

    public List<Role> GetUserRoles(int userId)
    {
        return _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToList();
    }
}
