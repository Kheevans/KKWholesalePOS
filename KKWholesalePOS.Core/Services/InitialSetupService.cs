
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class InitialSetupService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManagementService _userManagementService;
    private readonly RoleManagementService _roleManagementService;
    private readonly PermissionManagementService _permissionManagementService;
    private readonly AuditLogService _auditLogService;

    public InitialSetupService(
        IUserRepository userRepository,
        UserManagementService userManagementService,
        RoleManagementService roleManagementService,
        PermissionManagementService permissionManagementService,
        AuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
        _permissionManagementService = permissionManagementService;
        _auditLogService = auditLogService;
    }

    public User CreateInitialAdministrator(
        string username,
        string password)
    {
        if (_userRepository.HasAnyUsers())
        {
            throw new InvalidOperationException(
                "Initial setup has already been completed.");
        }

        var user = _userManagementService.CreateInitialUser(
            username,
            password);

        var ownerRole = _roleManagementService.CreateInitialRole(
            "Owner",
            "Business owner with full system access.");

        _roleManagementService.AssignInitialRoleToUser(
            user.Id,
            ownerRole.Id);

        _auditLogService.LogAction(
            user.Id,
            "InitialAdministratorCreated",
            "User",
            user.Id.ToString(),
            "Initial administrator account created.");

        return user;
    }

    public void EnsureDefaultPermissions()
    {
        var defaultPermissions = new Dictionary<string, string>
    {
        { "Dashboard.View", "Access the business dashboard." },
        { "Sales.Process", "Process regular sales." },
        { "Customers.Manage", "Create and manage customers." },
        { "Credit.Manage", "Manage credit accounts and payments." },
        { "Consignments.Issue", "Issue consignment goods." },
        { "Consignments.Return", "Process consignment returns." },
        { "Inventory.View", "View inventory and stock information." },
        { "Inventory.Receive", "Record received stock." },
        { "Inventory.Adjust", "Adjust stock quantities." },
        { "Reports.View", "View business reports." },
        { "Reports.Export", "Export business reports." },
        { "Users.Manage", "Create and manage user accounts." },
        { "Roles.Manage", "Create and manage system roles." },
        { "Permissions.Manage", "Assign and remove role permissions." },
        { "AuditLog.View", "View system activity and audit records." },
        { "Settings.Manage", "Manage business and system settings." }
    };

        var existingPermissions =
            _permissionManagementService.GetAllPermissionsForInitialSetup();

        foreach (var item in defaultPermissions)
        {
            if (!existingPermissions.Any(
                p => p.Name.Equals(
                    item.Key,
                    StringComparison.OrdinalIgnoreCase)))
            {
                _permissionManagementService.CreateInitialPermission(
                    item.Key,
                    item.Value);
            }
        }

        // Give the Owner role all default permissions.
        var ownerRole = _roleManagementService.GetAllRolesForInitialSetup()
            .FirstOrDefault(r => r.Name.Equals(
                "Owner",
                StringComparison.OrdinalIgnoreCase));

        if (ownerRole != null)
        {
            var allPermissions =
                _permissionManagementService.GetAllPermissionsForInitialSetup();

            foreach (var permission in allPermissions)
            {
                _permissionManagementService.AssignInitialPermissionToRole(
                    ownerRole.Id,
                    permission.Id);
            }
        }
    }
}