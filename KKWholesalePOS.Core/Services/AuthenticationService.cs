using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Services;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;

    public AuthenticationService(
        IUserRepository userRepository,
        PasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public User? Authenticate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = _userRepository.GetByUsername(username);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        bool passwordIsValid = _passwordService.VerifyPassword(
            user,
            password);

        if (!passwordIsValid)
        {
            return null;
        }

        return user;
    }
}
