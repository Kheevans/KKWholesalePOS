using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Core.Models.Settings;

namespace KKWholesalePOS.Core.Services;

public class BusinessSettingsService
{
    private readonly IBusinessSettingsRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public BusinessSettingsService(
        IBusinessSettingsRepository repository,
        UserSessionService userSession,
        AuthorizationService authorizationService,
        AuditLogService auditLogService)
    {
        _repository = repository;
        _userSession = userSession;
        _authorizationService = authorizationService;
        _auditLogService = auditLogService;
    }

    public BusinessSettings GetSettings()
    {
        RequirePermission("Settings.Manage");

        var settings = _repository.GetSettings();

        if (settings != null)
        {
            return settings;
        }

        settings = new BusinessSettings();

        _repository.Add(settings);
        _repository.SaveChanges();

        return settings;
    }

    public void UpdateSettings(BusinessSettings settings)
    {
        RequirePermission("Settings.Manage");

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.BusinessName))
        {
            throw new ArgumentException(
                "Business name is required.");
        }

        if (settings.DefaultMinimumStockLevel < 0)
        {
            throw new ArgumentException(
                "Default minimum stock level cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(settings.CurrencyCode))
        {
            throw new ArgumentException(
                "Currency code is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.DateFormat))
        {
            throw new ArgumentException(
                "Date format is required.");
        }

        _repository.SaveChanges();

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id,
            "BusinessSettingsUpdated",
            "BusinessSettings",
            settings.Id.ToString(),
            $"Business settings updated for '{settings.BusinessName}'.");
    }

    private void RequirePermission(string permissionName)
    {
        User? currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(
                currentUser,
                permissionName))
        {
            throw new UnauthorizedAccessException(
                $"You do not have permission to perform this action. Required permission: {permissionName}");
        }
    }
}