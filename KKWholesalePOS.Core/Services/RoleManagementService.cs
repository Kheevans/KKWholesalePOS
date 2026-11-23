
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class RoleManagementService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    internal List<Role> GetAllRolesForInitialSetup()
    {
        return _roleRepository.GetAll();
    }
    public RoleManagementService(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userSession = userSession;
        _authorizationService = authorizationService;
    }

    private bool HasPermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        return currentUser != null &&
               _authorizationService.HasPermission(
                   currentUser,
                   permissionName);
    }

    private void RequirePermission(string permissionName)
    {
        if (!HasPermission(permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
    }
    public List<Role> GetAllRoles()
    {
        if (!HasPermission("Roles.Manage") &&
            !HasPermission("Permissions.Manage"))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view roles.");
        }

        return _roleRepository.GetAll();
    }

    public Role CreateRole(string name, string description)
    {
        RequirePermission("Roles.Manage");
        return CreateRoleCore(name, description);
    }

    // Used only during initial system setup, before login.
    internal Role CreateInitialRole(string name, string description)
    {
        return CreateRoleCore(name, description);
    }

    private Role CreateRoleCore(string name, string description)
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
        RequirePermission("Roles.Manage");
        AssignRoleToUserCore(userId, roleId);
    }

    // Used only during initial system setup, before login.
    internal void AssignInitialRoleToUser(int userId, int roleId)
    {
        AssignRoleToUserCore(userId, roleId);
    }

    private void AssignRoleToUserCore(int userId, int roleId)
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
        RequirePermission("Roles.Manage");

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
        if (!HasPermission("Roles.Manage") &&
            !HasPermission("Users.Manage"))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view user roles.");
        }

        if (_userRepository.GetById(userId) == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        return _userRepository.GetUserRoles(userId);
    }
}