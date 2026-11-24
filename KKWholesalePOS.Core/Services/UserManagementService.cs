using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public UserManagementService(
        IUserRepository userRepository,
        PasswordService passwordService,
        UserSessionService userSession,
        AuthorizationService authorizationService,
        AuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _userSession = userSession;
        _authorizationService = authorizationService;
        _auditLogService = auditLogService;
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

    public List<User> GetAllUsers()
    {
        if (!HasPermission("Users.Manage") &&
            !HasPermission("Roles.Manage"))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view users.");
        }

        return _userRepository.GetAll();
    }

    public User CreateUser(string username, string password)
    {
        RequirePermission("Users.Manage");
        var user = CreateUserCore(username, password);

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id,
            "UserCreated",
            "User",
            user.Id.ToString(),
            $"Created user '{user.Username}'.");

        return user;
    }

    // Used only during initial system setup, before login.
    internal User CreateInitialUser(string username, string password)
    {
        return CreateUserCore(username, password);
    }

    private User CreateUserCore(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Username cannot be empty.",
                nameof(username));
        }

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.",
                nameof(password));
        }

        username = username.Trim();

        if (_userRepository.GetByUsername(username) != null)
        {
            throw new InvalidOperationException(
                "A user with this username already exists.");
        }

        var user = new User
        {
            Username = username,
            IsActive = true
        };

        user.PasswordHash = _passwordService.HashPassword(
            user,
            password);

        _userRepository.Add(user);
        _userRepository.SaveChanges();

        return user;
    }

    public void DeactivateUser(int userId)
    {
        RequirePermission("Users.Manage");

        var user = _userRepository.GetById(userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        user.IsActive = false;

        _userRepository.Update(user);
        _userRepository.SaveChanges();

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id,
            "UserDeactivated",
            "User",
            user.Id.ToString(),
            $"Deactivated user '{user.Username}'.");
    }

    public void ActivateUser(int userId)
    {
        RequirePermission("Users.Manage");

        var user = _userRepository.GetById(userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                "User could not be found.");
        }

        user.IsActive = true;

        _userRepository.Update(user);
        _userRepository.SaveChanges();

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id,
            "UserActivated",
            "User",
            user.Id.ToString(),
            $"Activated user '{user.Username}'.");
    }
}