
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class InitialSetupService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManagementService _userManagementService;
    private readonly RoleManagementService _roleManagementService;
    private readonly AuditLogService _auditLogService;

    public InitialSetupService(
        IUserRepository userRepository,
        UserManagementService userManagementService,
        RoleManagementService roleManagementService,
        AuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
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

        var user = _userManagementService.CreateUser(
            username,
            password);

        var ownerRole = _roleManagementService.CreateRole(
            "Owner",
            "Business owner with full system access.");

        _roleManagementService.AssignRoleToUser(
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
}
