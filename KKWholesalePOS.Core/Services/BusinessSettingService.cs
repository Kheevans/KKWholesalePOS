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
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
        _userSession = userSession ??
            throw new ArgumentNullException(nameof(userSession));
        _authorizationService = authorizationService ??
            throw new ArgumentNullException(nameof(authorizationService));
        _auditLogService = auditLogService ??
            throw new ArgumentNullException(nameof(auditLogService));
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

        ArgumentNullException.ThrowIfNull(settings);

        string businessName = settings.BusinessName?.Trim() ?? string.Empty;
        string currencyCode = settings.CurrencyCode?.Trim() ?? string.Empty;
        string dateFormat = settings.DateFormat?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException("Business name is required.");
        }

        if (settings.DefaultMinimumStockLevel < 0)
        {
            throw new ArgumentException(
                "Default minimum stock level cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.");
        }

        if (string.IsNullOrWhiteSpace(dateFormat))
        {
            throw new ArgumentException("Date format is required.");
        }

        var existing = _repository.GetSettings();
        if (existing == null)
        {
            existing = new BusinessSettings();
            _repository.Add(existing);
        }

        existing.BusinessName = businessName;
        existing.BusinessAddress = settings.BusinessAddress?.Trim() ?? string.Empty;
        existing.PhoneNumber = settings.PhoneNumber?.Trim() ?? string.Empty;
        existing.EmailAddress = settings.EmailAddress?.Trim() ?? string.Empty;
        existing.ReceiptHeader = settings.ReceiptHeader?.Trim() ?? string.Empty;
        existing.ReceiptFooter = settings.ReceiptFooter?.Trim() ?? string.Empty;
        existing.ShowBusinessContactDetails = settings.ShowBusinessContactDetails;
        existing.CurrencyCode = currencyCode;
        existing.DateFormat = dateFormat;
        existing.DefaultMinimumStockLevel = settings.DefaultMinimumStockLevel;

        _repository.SaveChanges();

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id,
            "BusinessSettingsUpdated",
            "BusinessSettings",
            existing.Id.ToString(),
            $"Business settings updated for '{existing.BusinessName}'.");
    }

    private void RequirePermission(string permissionName)
    {
        User? currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(currentUser, permissionName))
        {
            throw new UnauthorizedAccessException(
                $"You do not have permission to perform this action. Required permission: {permissionName}");
        }
    }
}