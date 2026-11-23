using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class UserSessionService
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser != null;

    public void StartSession(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "Inactive users cannot start a session.");
        }

        CurrentUser = user;
    }

    public void EndSession()
    {
        CurrentUser = null;
    }
}