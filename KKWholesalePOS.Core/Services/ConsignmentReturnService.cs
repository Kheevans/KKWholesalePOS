using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Consignment;

namespace KKWholesalePOS.Core.Services;

public record ConsignmentLineStatus(
    ConsignmentLine Line, int AcceptedReturned, int Outstanding);

public record ConsignmentReturnView(
    Consignment Consignment,
    IReadOnlyList<ConsignmentLineStatus> Lines,
    ConsignmentSettlement Settlement);

public record ReturnRequestLine(
    int ConsignmentLineId, int QuantityReturned, int QuantityAccepted);

public record ConsignmentSummaryItem(
    int ConsignmentId,
    string ReferenceNumber,
    DateTime IssuedAt,
    decimal FinalBalance,
    decimal Outstanding,
    bool IsSettled,
    string DisplayText);

public class ConsignmentReturnService
{
    private const string ReturnPermission = "Consignments.Return";

    private readonly IConsignmentReturnRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService? _auditLogService;

    public ConsignmentReturnService(
        IConsignmentReturnRepository repository,
        UserSessionService userSession,
        AuthorizationService authorizationService,
        AuditLogService? auditLogService = null)
    {
        _repository = repository;
        _userSession = userSession;
        _authorizationService = authorizationService;
        _auditLogService = auditLogService;
    }

    private int RequirePermission(string permissionName)
    {
        var user = _userSession.CurrentUser;

        if (user == null ||
            !_authorizationService.HasPermission(user, permissionName))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }

        return user.Id;
    }

    // SR-04.1
    public ConsignmentReturnView? FindConsignment(string referenceNumber)
    {
        RequirePermission(ReturnPermission);

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("Consignment reference is required.");

        var consignment =
            _repository.FindConsignmentByReference(referenceNumber.Trim());

        return consignment == null ? null : BuildView(consignment);
    }

    public IReadOnlyList<ConsignmentSummaryItem> GetCustomerConsignments(
        int customerId,
        string currencyPrefix = "J$",
        string dateFormat = "yyyy-MM-dd")
    {
        RequirePermission(ReturnPermission);

        if (customerId <= 0)
            throw new ArgumentException("A valid customer must be selected.");

        var consignments = _repository.GetConsignmentsByCustomerId(customerId);
        var summaries = new List<ConsignmentSummaryItem>(consignments.Count);

        foreach (var c in consignments)
        {
            var view = BuildView(c);
            var st = view.Settlement;
            string statusTag = st.IsSettled ? "Settled" : $"Owed: {currencyPrefix}{st.Outstanding:N2}";
            string display = $"{c.ReferenceNumber} ({c.IssuedAt.ToLocalTime().ToString(dateFormat)}) - {statusTag}";

            summaries.Add(new ConsignmentSummaryItem(
                c.Id,
                c.ReferenceNumber,
                c.IssuedAt,
                st.FinalBalance,
                st.Outstanding,
                st.IsSettled,
                display));
        }

        return summaries;
    }

    public IReadOnlyList<ConsignmentReturn> GetReturnHistory(int consignmentId)
    {
        RequirePermission(ReturnPermission);
        return _repository.GetReturns(consignmentId);
    }

    public IReadOnlyList<ConsignmentPayment> GetPaymentHistory(int consignmentId)
    {
        RequirePermission(ReturnPermission);
        return _repository.GetPayments(consignmentId);
    }

    // SR-04.2, 04.3, 04.4 (stock restore happens inside the repository)
    public ConsignmentReturn ProcessReturn(
        string referenceNumber, IReadOnlyList<ReturnRequestLine> requested)
    {
        var userId = RequirePermission(ReturnPermission);

        var view = FindConsignment(referenceNumber)
            ?? throw new InvalidOperationException("Consignment not found.");

        if (requested == null || requested.Count == 0)
            throw new ArgumentException("Add at least one returned item.");

        if (requested.GroupBy(r => r.ConsignmentLineId).Any(g => g.Count() > 1))
            throw new ArgumentException("A product appears twice in this return.");

        var statuses = view.Lines.ToDictionary(s => s.Line.Id);
        var newLines = new List<ConsignmentReturnLine>();

        foreach (var req in requested)
        {
            if (!statuses.TryGetValue(req.ConsignmentLineId, out var status))
                throw new InvalidOperationException(
                    "An item does not belong to this consignment.");

            if (req.QuantityReturned <= 0)
                throw new ArgumentException(
                    $"Returned quantity for {status.Line.ProductName} must be greater than zero.");

            if (req.QuantityReturned > status.Outstanding)
                throw new InvalidOperationException(
                    $"{status.Line.ProductName}: only {status.Outstanding} still outstanding.");

            if (req.QuantityAccepted < 0 ||
                req.QuantityAccepted > req.QuantityReturned)
                throw new ArgumentException(
                    $"Accepted quantity for {status.Line.ProductName} is invalid.");

            newLines.Add(new ConsignmentReturnLine
            {
                ConsignmentLineId = status.Line.Id,
                QuantityReturned = req.QuantityReturned,
                QuantityAccepted = req.QuantityAccepted,
                // Price from the original issuance, not today's price
                UnitPrice = status.Line.UnitPrice
            });
        }

        var newReturnsValue = view.Settlement.AcceptedReturnsValue +
                              newLines.Sum(l => l.AcceptedValue);
        try
        {
            ConsignmentSettlementCalculator.Calculate(
                view.Consignment.OriginalGoodsValue,
                newReturnsValue,
                view.Settlement.TotalPaid);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "These returns would put the account in overpayment. " +
                "Refunds are not supported; speak to the owner.");
        }

        var savedReturn = _repository.SaveReturn(new ConsignmentReturn
        {
            ConsignmentId = view.Consignment.Id,
            UserId = userId,
            ReturnDate = DateTime.UtcNow,
            ReferenceNumber = "RET-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
            Lines = newLines
        });

        int totalAccepted = newLines.Sum(l => l.QuantityAccepted);
        decimal acceptedValue = newLines.Sum(l => l.AcceptedValue);

        _auditLogService?.LogAction(
            userId,
            "Consignment Return",
            "Consignment",
            view.Consignment.Id.ToString(),
            $"Processed return {savedReturn.ReferenceNumber} for {view.Consignment.ReferenceNumber} " +
            $"({totalAccepted} accepted item(s), value {acceptedValue:N2}).");

        return savedReturn;
    }

    // SR-04.5
    public ConsignmentPayment RecordPayment(
        string referenceNumber, decimal amount, string paymentMethod)
    {
        var userId = RequirePermission(ReturnPermission);

        var view = FindConsignment(referenceNumber)
            ?? throw new InvalidOperationException("Consignment not found.");

        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        if (amount <= 0m)
            throw new ArgumentException("Payment must be greater than zero.");

        if (amount > view.Settlement.Outstanding)
            throw new InvalidOperationException(
                $"Payment exceeds the amount owed ({view.Settlement.Outstanding:N2}).");

        string method = string.IsNullOrWhiteSpace(paymentMethod)
            ? "Cash"
            : paymentMethod.Trim();

        var savedPayment = _repository.SavePayment(new ConsignmentPayment
        {
            ConsignmentId = view.Consignment.Id,
            UserId = userId,
            PaymentDate = DateTime.UtcNow,
            Amount = amount,
            PaymentMethod = method
        });

        _auditLogService?.LogAction(
            userId,
            "Consignment Payment",
            "Consignment",
            view.Consignment.Id.ToString(),
            $"Recorded {method} payment of {amount:N2} for {view.Consignment.ReferenceNumber}.");

        return savedPayment;
    }

    private ConsignmentReturnView BuildView(Consignment consignment)
    {
        var returns = _repository.GetReturns(consignment.Id);
        var payments = _repository.GetPayments(consignment.Id);

        var returnLines = returns.SelectMany(r => r.Lines).ToList();

        var acceptedByLine = returnLines
            .GroupBy(l => l.ConsignmentLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityAccepted));

        var statuses = consignment.Lines.Select(line =>
        {
            acceptedByLine.TryGetValue(line.Id, out var accepted);
            return new ConsignmentLineStatus(
                line, accepted, line.Quantity - accepted);
        }).ToList();

        var settlement = ConsignmentSettlementCalculator.Calculate(
            consignment.OriginalGoodsValue,
            returnLines.Sum(l => l.AcceptedValue),
            payments.Sum(p => p.Amount));

        return new ConsignmentReturnView(consignment, statuses, settlement);
    }
}

// ---- Settlement math (SR-04.3, SR-04.5) ----

public record ConsignmentSettlement(
    decimal OriginalTotal,
    decimal Fee,
    decimal AcceptedReturnsValue,
    decimal FinalBalance,
    decimal TotalPaid,
    decimal Outstanding,
    bool IsSettled);

public static class ConsignmentSettlementCalculator
{
    public const decimal FeeRate = 0.05m;

    public static ConsignmentSettlement Calculate(
        decimal originalTotal,
        decimal acceptedReturnsValue,
        decimal totalPaid)
    {
        if (originalTotal < 0 || acceptedReturnsValue < 0 || totalPaid < 0)
            throw new ArgumentException("Amounts cannot be negative.");

        if (acceptedReturnsValue > originalTotal)
            throw new ArgumentException("Returns cannot exceed the original total.");

        // Fee is always 5% of the ORIGINAL total
        var fee = Math.Round(originalTotal * FeeRate, 2, MidpointRounding.AwayFromZero);
        var finalBalance = originalTotal - acceptedReturnsValue + fee;
        var outstanding = finalBalance - totalPaid;

        if (outstanding < 0)
            throw new ArgumentException("Payments exceed the amount owed.");

        return new ConsignmentSettlement(
            originalTotal, fee, acceptedReturnsValue,
            finalBalance, totalPaid, outstanding, outstanding == 0);
    }
}