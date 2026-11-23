
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;

    public UserManagementService(
        IUserRepository userRepository,
        PasswordService passwordService,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
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
        return CreateUserCore(username, password);
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
    }
}
