using KKWholesalePOS.Core.Models.Others;
using Microsoft.AspNetCore.Identity;

namespace KKWholesalePOS.Core.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));
        }

        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}
