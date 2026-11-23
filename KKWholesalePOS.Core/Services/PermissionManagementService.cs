
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class PermissionManagementService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    internal List<Permission> GetAllPermissionsForInitialSetup()
    {
        return _permissionRepository.GetAll();
    }
    public PermissionManagementService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userSession = userSession;
        _authorizationService = authorizationService;
    }

    private void RequirePermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(
                currentUser,
                permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
    }

    public Permission CreatePermission(string name, string description)
    {
        RequirePermission("Permissions.Manage");
        return CreatePermissionCore(name, description);
    }

    internal Permission CreateInitialPermission(string name, string description)
    {
        return CreatePermissionCore(name, description);
    }

    private Permission CreatePermissionCore(string name, string description)
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
        RequirePermission("Permissions.Manage");
        AssignPermissionToRoleCore(roleId, permissionId);
    }

    internal void AssignInitialPermissionToRole(int roleId, int permissionId)
    {
        AssignPermissionToRoleCore(roleId, permissionId);
    }

    private void AssignPermissionToRoleCore(int roleId, int permissionId)
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
        RequirePermission("Permissions.Manage");

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
        RequirePermission("Permissions.Manage");

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
        RequirePermission("Permissions.Manage");

        return _permissionRepository.GetAll();
    }
}
