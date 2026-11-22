using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;

    public UserManagementService(
        IUserRepository userRepository,
        PasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public User CreateUser(string username, string password)
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
