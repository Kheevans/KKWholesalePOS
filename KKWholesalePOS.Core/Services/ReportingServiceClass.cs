
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Reports;

namespace KKWholesalePOS.Core.Services;

public class ReportingService
{
    private readonly IReportingRepository _reportingRepository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;

    public ReportingService(
        IReportingRepository reportingRepository,
        UserSessionService userSession,
        AuthorizationService authorizationService)
    {
        _reportingRepository = reportingRepository;
        _userSession = userSession;
        _authorizationService = authorizationService;
    }

    private void RequirePermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(
                currentUser,
                permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
    }

    public SalesReportResult GetSalesReport(
        DateTime startDate,
        DateTime endDate)
    {
        RequirePermission("Reports.View");

        ValidateDateRange(startDate, endDate);

        return _reportingRepository.GetSalesReport(
            startDate.Date,
            endDate.Date);
    }

    public CreditStatementResult GetCreditStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        RequirePermission("Reports.View");

        ValidateCustomerAndDateRange(
            customerId, startDate, endDate);

        return _reportingRepository.GetCreditStatement(
            customerId,
            startDate.Date,
            endDate.Date);
    }

    public ConsignmentStatementResult GetConsignmentStatement(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        RequirePermission("Reports.View");

        ValidateCustomerAndDateRange(
            customerId, startDate, endDate);

        return _reportingRepository.GetConsignmentStatement(
            customerId,
            startDate.Date,
            endDate.Date);
    }

    public InventoryReportResult GetInventoryReport()
    {
        RequirePermission("Reports.View");

        return _reportingRepository.GetInventoryReport();
    }

    private static void ValidateCustomerAndDateRange(
        int customerId,
        DateTime startDate,
        DateTime endDate)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException(
                "A valid customer must be selected.",
                nameof(customerId));
        }

        ValidateDateRange(startDate, endDate);
    }

    private static void ValidateDateRange(
        DateTime startDate,
        DateTime endDate)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException(
                "The start date cannot be later than the end date.");
        }
    }
}
