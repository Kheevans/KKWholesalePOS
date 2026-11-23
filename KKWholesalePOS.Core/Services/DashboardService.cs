
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Dashboard;

namespace KKWholesalePOS.Core.Services;

public class DashboardService
{
    private readonly IDashboardRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;

    public DashboardService(
        IDashboardRepository repository,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _repository = repository;
        _userSession = userSession;
        _authorizationService = authorizationService;
    }

    public DashboardSummary GetDashboardSummary()
    {
        RequirePermission("Dashboard.View");

        return _repository.GetDashboardSummary();
    }

    private void RequirePermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(
                currentUser, permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
    }
}
