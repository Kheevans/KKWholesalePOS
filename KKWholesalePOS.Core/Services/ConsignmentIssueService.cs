using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;

namespace KKWholesalePOS.Core.Services;

public class ConsignmentIssueService
{
    private const string IssuePermission = "Consignments.Issue";

    private readonly IConsignmentIssueRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public ConsignmentIssueService(
        IConsignmentIssueRepository repository,
        UserSessionService userSession,
        AuthorizationService authorizationService,
        AuditLogService auditLogService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    private int RequirePermission(string permissionName)
    {
        var currentUser = _userSession.CurrentUser;

        if (currentUser == null ||
            !_authorizationService.HasPermission(currentUser, permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }

        return currentUser.Id;
    }

    public async Task<Consignment> IssueConsignmentAsync(
        int customerId,
        List<(int ProductId, int Quantity, decimal Price)> requestedLines)
    {
        int userId = RequirePermission(IssuePermission);

        if (customerId <= 0)
            throw new ArgumentException("A valid customer must be selected.", nameof(customerId));

        if (requestedLines == null || requestedLines.Count == 0)
            throw new InvalidOperationException("At least one line item is required.");

        decimal originalGoodsValue = 0m;
        var lines = new List<ConsignmentLine>(requestedLines.Count);

        foreach (var reqLine in requestedLines)
        {
            var product = _repository.GetProduct(reqLine.ProductId)
                ?? throw new InvalidOperationException($"Product ID {reqLine.ProductId} not found.");

            if (reqLine.Quantity <= 0)
                throw new InvalidOperationException($"Quantity for '{product.Name}' must be greater than zero.");

            if (reqLine.Quantity > product.StockQuantity)
                throw new InvalidOperationException($"Only {product.StockQuantity} unit(s) of '{product.Name}' are available.");

            if (reqLine.Price < 0m)
                throw new ArgumentException($"Price for '{product.Name}' cannot be negative.");

            decimal unitPrice = Math.Round(reqLine.Price, 2, MidpointRounding.AwayFromZero);
            decimal lineTotal = Math.Round(reqLine.Quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
            originalGoodsValue += lineTotal;

            lines.Add(new ConsignmentLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode ?? string.Empty,
                Quantity = reqLine.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        originalGoodsValue = Math.Round(originalGoodsValue, 2, MidpointRounding.AwayFromZero);
        decimal consignmentFee = Math.Round(
            originalGoodsValue * ConsignmentSettlementCalculator.FeeRate,
            2,
            MidpointRounding.AwayFromZero);

        var consignment = new Consignment
        {
            CustomerId = customerId,
            UserId = userId,
            IssuedAt = DateTime.UtcNow,
            ReferenceNumber = $"CON-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}",
            OriginalGoodsValue = originalGoodsValue,
            ConsignmentFee = consignmentFee,
            Lines = lines
        };

        await _repository.SaveConsignmentAsync(consignment);

        _auditLogService.LogAction(
            userId: userId,
            action: "Consignment Issue",
            entityType: nameof(Consignment),
            entityId: consignment.Id.ToString(),
            details: $"Issued consignment {consignment.ReferenceNumber} to Customer ID {customerId} " +
                     $"({consignment.Lines.Count} line(s), goods value {originalGoodsValue:N2}, fee {consignmentFee:N2}).");

        return consignment;
    }
}