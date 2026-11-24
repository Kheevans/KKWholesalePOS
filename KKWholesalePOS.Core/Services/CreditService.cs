using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Sales;

namespace KKWholesalePOS.Core.Services;

public class CreditAccountSummary
{
    public int CustomerId { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal TotalPurchasedWithFees { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AvailableCredit => Math.Max(0m, CreditLimit - OutstandingBalance);
    public bool HasUnpaidInvoice { get; set; }
    public bool HasOverdueInvoice { get; set; }
    public DateTime? ActiveDueDate { get; set; }
}

public class CreditService
{
    private readonly ICreditRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    private const decimal DefaultCreditLimit = 100000m;
    private const decimal CreditFeeRate = 0.05m;
    private const int DefaultRepaymentDays = 14;

    private static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Cash",
            "Card",
            "Bank Transfer",
            "Cheque",
            "Other"
        };

    public CreditService(
        ICreditRepository repository,
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

    public List<CreditSale> GetCustomerAccount(int customerId)
    {
        RequirePermission("Credit.Manage");

        if (customerId <= 0)
            throw new ArgumentException("Invalid customer ID.", nameof(customerId));

        return _repository.GetByCustomerId(customerId);
    }

    public CreditAccountSummary GetCustomerCreditSummary(int customerId)
    {
        RequirePermission("Credit.Manage");

        if (customerId <= 0)
            throw new ArgumentException("Invalid customer ID.", nameof(customerId));

        var sales = _repository.GetByCustomerId(customerId);
        decimal totalWithFees = 0m;
        decimal totalPaid = 0m;
        bool hasUnpaid = false;
        bool hasOverdue = false;
        DateTime? activeDueDate = null;

        foreach (var sale in sales)
        {
            var payments = _repository.GetPaymentsByCreditSaleId(sale.Id);
            decimal paidForSale = payments.Sum(p => p.Amount);
            decimal balanceForSale = sale.TotalAmount - paidForSale;

            totalWithFees += sale.TotalAmount;
            totalPaid += paidForSale;

            if (balanceForSale > 0m)
            {
                hasUnpaid = true;
                activeDueDate = sale.DueDate;

                if (DateTime.UtcNow.Date > sale.DueDate.Date)
                {
                    hasOverdue = true;
                }
            }
        }

        decimal outstanding = Math.Max(0m, totalWithFees - totalPaid);

        return new CreditAccountSummary
        {
            CustomerId = customerId,
            CreditLimit = DefaultCreditLimit,
            TotalPurchasedWithFees = totalWithFees,
            TotalPaid = totalPaid,
            OutstandingBalance = outstanding,
            HasUnpaidInvoice = hasUnpaid,
            HasOverdueInvoice = hasOverdue,
            ActiveDueDate = activeDueDate
        };
    }

    public List<CreditPayment> GetPaymentHistory(int creditSaleId)
    {
        RequirePermission("Credit.Manage");

        if (creditSaleId <= 0)
            throw new ArgumentException("Invalid credit sale ID.", nameof(creditSaleId));

        return _repository.GetPaymentsByCreditSaleId(creditSaleId);
    }

    public CreditSale CreateCreditSale(
        int customerId,
        List<SaleRequestLine> items)
    {
        int userId = RequirePermission("Credit.Manage");

        if (customerId <= 0)
            throw new ArgumentException("Invalid customer ID.", nameof(customerId));

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("The purchase must contain items.");

        if (_repository.HasUnpaidCredit(customerId))
        {
            throw new InvalidOperationException(
                "Customer already has an unpaid credit invoice.");
        }

        var requestedQuantities = items
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Quantity));

        if (requestedQuantities.Any(item => item.Key <= 0 || item.Value <= 0))
            throw new InvalidOperationException(
                "Each product and quantity must be valid.");

        var products = new Dictionary<int, Product>();

        foreach (int productId in requestedQuantities.Keys)
        {
            var product = _repository.GetProductById(productId);

            if (product == null)
                throw new InvalidOperationException(
                    $"Product with ID {productId} was not found.");

            if (requestedQuantities[productId] > product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for {product.Name}. " +
                    $"Available: {product.StockQuantity}; " +
                    $"requested: {requestedQuantities[productId]}.");
            }

            if (product.SellingPrice < 0m)
                throw new InvalidOperationException(
                    $"The selling price for {product.Name} is invalid.");

            products.Add(productId, product);
        }

        decimal purchaseAmount = 0m;
        var lines = new List<CreditSaleLine>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                throw new InvalidOperationException(
                    "Quantity must be greater than zero.");

            Product product = products[item.ProductId];
            decimal lineTotal = Math.Round(
                product.SellingPrice * item.Quantity,
                2,
                MidpointRounding.AwayFromZero);

            purchaseAmount += lineTotal;

            lines.Add(new CreditSaleLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                UnitPrice = product.SellingPrice,
                Quantity = item.Quantity,
                LineTotal = lineTotal
            });
        }

        purchaseAmount = Math.Round(purchaseAmount, 2, MidpointRounding.AwayFromZero);

        if (purchaseAmount > DefaultCreditLimit)
        {
            throw new InvalidOperationException(
                $"Purchase exceeds the J${DefaultCreditLimit:N2} credit limit.");
        }

        decimal creditFee = Math.Round(
            purchaseAmount * CreditFeeRate,
            2,
            MidpointRounding.AwayFromZero);

        var issuedAt = DateTime.UtcNow;

        var sale = new CreditSale
        {
            CustomerId = customerId,
            UserId = userId,
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(DefaultRepaymentDays),
            PurchaseAmount = purchaseAmount,
            CreditFee = creditFee,
            ReferenceNumber = $"CR-{Guid.NewGuid():N}",
            Lines = lines
        };

        _repository.CreateCreditSaleWithStock(sale, userId);

        _auditLogService.LogAction(
            userId,
            "CreditSaleCreated",
            "CreditSale",
            sale.Id.ToString(),
            $"Credit sale {sale.ReferenceNumber} created for Customer ID {customerId}. " +
            $"Principal: J${sale.PurchaseAmount:N2}, Fee: J${sale.CreditFee:N2}, Total: J${sale.TotalAmount:N2}, Due: {sale.DueDate:yyyy-MM-dd}.");

        return sale;
    }

    public CreditPayment RecordPayment(
        int creditSaleId,
        decimal amount,
        string paymentMethod,
        string? referenceNumber = null)
    {
        int userId = RequirePermission("Credit.Manage");

        if (creditSaleId <= 0)
            throw new ArgumentException(
                "Invalid credit sale ID.", nameof(creditSaleId));

        if (amount <= 0m)
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");

        if (amount != decimal.Round(amount, 2))
            throw new InvalidOperationException(
                "Payment amount cannot contain more than two decimal places.");

        string normalizedMethod = paymentMethod?.Trim() ?? string.Empty;

        if (!AllowedPaymentMethods.Contains(normalizedMethod))
            throw new InvalidOperationException(
                "Please select a valid payment method.");

        var sale = _repository.GetById(creditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        var payments = _repository.GetPaymentsByCreditSaleId(creditSaleId);
        decimal paidAmount = payments.Sum(payment => payment.Amount);
        decimal outstanding = sale.TotalAmount - paidAmount;

        if (outstanding <= 0m)
            throw new InvalidOperationException(
                "This credit account has already been fully paid.");

        if (amount > outstanding)
            throw new InvalidOperationException(
                $"Payment exceeds the outstanding balance of J${outstanding:N2}.");

        var payment = new CreditPayment
        {
            CreditSaleId = creditSaleId,
            UserId = userId,
            PaymentDate = DateTime.UtcNow,
            Amount = amount,
            PaymentMethod = normalizedMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber)
                ? null
                : referenceNumber.Trim()
        };

        _repository.AddPaymentWithinBalance(payment);

        decimal remainingAfter = outstanding - amount;

        _auditLogService.LogAction(
            userId,
            "CreditPaymentRecorded",
            "CreditPayment",
            payment.Id.ToString(),
            $"Payment of J${amount:N2} ({normalizedMethod}) recorded on Credit Sale #{creditSaleId} ({sale.ReferenceNumber}). " +
            $"Remaining balance: J${remainingAfter:N2}.");

        return payment;
    }

    public void ModifyCreditAmount(
        int creditSaleId,
        decimal newPurchaseAmount,
        string reason)
    {
        int userId = RequirePermission("Credit.Modify");

        if (creditSaleId <= 0)
            throw new ArgumentException(
                "Invalid credit sale ID.", nameof(creditSaleId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "A reason is required when modifying a credit balance.", nameof(reason));

        if (newPurchaseAmount <= 0m ||
            newPurchaseAmount > DefaultCreditLimit ||
            newPurchaseAmount != decimal.Round(newPurchaseAmount, 2))
        {
            throw new InvalidOperationException(
                $"Credit amount must be between J$0.01 and J${DefaultCreditLimit:N2}, with no more than two decimal places.");
        }

        var sale = _repository.GetById(creditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        if (sale.Lines.Count > 0)
        {
            throw new InvalidOperationException(
                "This credit sale contains product lines. Its amount cannot be changed independently of those lines.");
        }

        decimal paidAmount = _repository
            .GetPaymentsByCreditSaleId(creditSaleId)
            .Sum(payment => payment.Amount);

        decimal newFee = Math.Round(
            newPurchaseAmount * CreditFeeRate,
            2,
            MidpointRounding.AwayFromZero);

        decimal newTotal = newPurchaseAmount + newFee;

        if (newTotal < paidAmount)
        {
            throw new InvalidOperationException(
                "The revised credit amount cannot be less than payments already made.");
        }

        decimal oldAmount = sale.PurchaseAmount;

        sale.PurchaseAmount = newPurchaseAmount;
        sale.CreditFee = newFee;

        _repository.UpdateCreditSale(sale);
        _repository.SaveChanges();

        _auditLogService.LogAction(
            userId,
            "CreditAmountModified",
            "CreditSale",
            creditSaleId.ToString(),
            $"Purchase amount changed from J${oldAmount:N2} to J${newPurchaseAmount:N2}. Reason: {reason.Trim()}");
    }

    public void ModifyPayment(
        int paymentId,
        decimal newAmount,
        string reason)
    {
        int userId = RequirePermission("Credit.Modify");

        if (paymentId <= 0)
            throw new ArgumentException(
                "Invalid payment ID.", nameof(paymentId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "A reason is required when modifying a credit payment.", nameof(reason));

        if (newAmount <= 0m || newAmount != decimal.Round(newAmount, 2))
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero and have no more than two decimal places.");
        }

        var payment = _repository.GetPaymentById(paymentId);

        if (payment == null)
            throw new InvalidOperationException("Payment not found.");

        var sale = _repository.GetById(payment.CreditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        var payments = _repository
            .GetPaymentsByCreditSaleId(payment.CreditSaleId);

        decimal otherPayments = payments
            .Where(existingPayment => existingPayment.Id != paymentId)
            .Sum(existingPayment => existingPayment.Amount);

        if (otherPayments + newAmount > sale.TotalAmount)
        {
            throw new InvalidOperationException(
                "The revised payment would cause total payments to exceed the credit sale amount.");
        }

        decimal oldAmount = payment.Amount;
        payment.Amount = newAmount;

        _repository.UpdatePayment(payment);
        _repository.SaveChanges();

        _auditLogService.LogAction(
            userId,
            "CreditPaymentModified",
            "CreditPayment",
            paymentId.ToString(),
            $"Payment changed from J${oldAmount:N2} to J${newAmount:N2}. Reason: {reason.Trim()}");
    }
}