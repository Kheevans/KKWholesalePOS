using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class PermissionManagementService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;

    public PermissionManagementService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
    }

    public Permission CreatePermission(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Permission name cannot be empty.",
                nameof(name));
        }

        name = name.Trim();

        if (_permissionRepository.GetByName(name) != null)
        {
            throw new InvalidOperationException(
                "A permission with this name already exists.");
        }

        var permission = new Permission
        {
            Name = name,
            Description = description?.Trim() ?? string.Empty
        };

        _permissionRepository.Add(permission);
        _permissionRepository.SaveChanges();

        return permission;
    }

    public void AssignPermissionToRole(int roleId, int permissionId)
    {
        var role = _roleRepository.GetById(roleId);

        if (role == null)
        {
            throw new InvalidOperationException(
                "Role could not be found.");
        }

        var permission = _permissionRepository.GetById(permissionId);

        if (permission == null)
        {
            throw new InvalidOperationException(
                "Permission could not be found.");
        }

        if (role.RolePermissions.Any(
            rp => rp.PermissionId == permissionId))
        {
            return;
        }

        role.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            Role = role,
            PermissionId = permissionId,
            Permission = permission
        });

        _roleRepository.Update(role);
        _roleRepository.SaveChanges();
    }

    public void RemovePermissionFromRole(int roleId, int permissionId)
    {
        var role = _roleRepository.GetById(roleId);

        if (role == null)
        {
            throw new InvalidOperationException(
                "Role could not be found.");
        }

        var rolePermission = role.RolePermissions.FirstOrDefault(
            rp => rp.PermissionId == permissionId);

        if (rolePermission == null)
        {
            return;
        }

        role.RolePermissions.Remove(rolePermission);

        _roleRepository.Update(role);
        _roleRepository.SaveChanges();
    }

    public List<Permission> GetRolePermissions(int roleId)
    {
        var role = _roleRepository.GetById(roleId);

        if (role == null)
        {
            throw new InvalidOperationException(
                "Role could not be found.");
        }

        return role.RolePermissions
            .Select(rp => rp.Permission)
            .ToList();
    }

    public List<Permission> GetAllPermissions()
    {
        return _permissionRepository.GetAll();
    }
}