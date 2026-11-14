using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Services;

public class LoginService
{
    private readonly AuthenticationService _authenticationService;
    private readonly UserSessionService _userSession;
    private readonly AuditLogService _auditLogService;

    public LoginService(
        AuthenticationService authenticationService,
        UserSessionService userSession,
        AuditLogService auditLogService)
    {
        _authenticationService = authenticationService;
        _userSession = userSession;
        _auditLogService = auditLogService;
    }

    public bool Login(string username, string password)
    {
        var user = _authenticationService.Authenticate(
            username,
            password);

        if (user == null)
        {
            _auditLogService.LogAction(
                null,
                "LoginFailed",
                "Authentication",
                null,
                $"Failed login attempt for username: {username}");

            return false;
        }

        _userSession.StartSession(user);

        _auditLogService.LogAction(
            user.Id,
            "Login",
            "Authentication",
            user.Id.ToString(),
            "User logged in successfully.");

        return true;
    }

    public void Logout()
    {
        var user = _userSession.CurrentUser;

        if (user == null)
        {
            return;
        }

        _auditLogService.LogAction(
            user.Id,
            "Logout",
            "Authentication",
            user.Id.ToString(),
            "User logged out.");

        _userSession.EndSession();
    }
}