using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Services;

public class RoleManagementService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;

    public RoleManagementService(
        IRoleRepository roleRepository,
        IUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
    }

    public Role CreateRole(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Role name cannot be empty.",
                nameof(name));
        }

        name = name.Trim();

        if (_roleRepository.GetByName(name) != null)
        {
            throw new InvalidOperationException(
                "A role with this name already exists.");
        }

        var role = new Role
        {
            Name = name,
            Description = description?.Trim() ?? string.Empty
        };

        _roleRepository.Add(role);
        _roleRepository.SaveChanges();

        return role;
    }

    public void AssignRoleToUser(int userId, int roleId)
    {
        if (_userRepository.GetById(userId) == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        if (_roleRepository.GetById(roleId) == null)
        {
            throw new InvalidOperationException(
                "Role could not be found.");
        }

        _userRepository.AssignRole(userId, roleId);
    }

    public void RemoveRoleFromUser(int userId, int roleId)
    {
        if (_userRepository.GetById(userId) == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        if (_roleRepository.GetById(roleId) == null)
        {
            throw new InvalidOperationException(
                "Role could not be found.");
        }

        _userRepository.RemoveRole(userId, roleId);
    }

    public List<Role> GetUserRoles(int userId)
    {
        if (_userRepository.GetById(userId) == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        return _userRepository.GetUserRoles(userId);
    }
}