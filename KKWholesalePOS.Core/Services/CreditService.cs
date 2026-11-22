
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Services;

public class CreditService
{
    private readonly ICreditRepository _repository;
    private readonly IAuditLogRepository _auditRepository;

    private const decimal CreditLimit = 100000m;
    private const decimal CreditFeeRate = 0.05m;

    public CreditService(
        ICreditRepository repository,
        IAuditLogRepository auditRepository)
    {
        _repository = repository;
        _auditRepository = auditRepository;
    }

    public List<CreditSale> GetCustomerAccount(int customerId)
    {
        if (customerId <= 0)
            throw new ArgumentException("Invalid customer ID.");

        return _repository.GetByCustomerId(customerId);
    }

    public List<CreditPayment> GetPaymentHistory(int creditSaleId)
    {
        if (creditSaleId <= 0)
            throw new ArgumentException("Invalid credit sale ID.");

        return _repository.GetPaymentsByCreditSaleId(creditSaleId);
    }

    public CreditSale CreateCreditSale(
        int customerId,
        int userId,
        List<SaleRequestLine> items)
    {
        if (customerId <= 0 || userId <= 0)
            throw new ArgumentException("Invalid customer or user.");

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("The purchase must contain items.");

        if (_repository.HasUnpaidCredit(customerId))
            throw new InvalidOperationException(
                "Customer has an unpaid credit account.");

        decimal purchaseAmount = 0;
        var lines = new List<CreditSaleLine>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                throw new InvalidOperationException(
                    "Quantity must be greater than zero.");

            var product = _repository.GetProductById(item.ProductId);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            if (item.Quantity > product.StockQuantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {product.Name}.");

            decimal lineTotal = product.SellingPrice * item.Quantity;
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

        if (purchaseAmount > CreditLimit)
            throw new InvalidOperationException(
                "Purchase exceeds the J$100,000 credit limit.");

        var sale = new CreditSale
        {
            CustomerId = customerId,
            UserId = userId,
            IssuedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            PurchaseAmount = purchaseAmount,
            CreditFee = purchaseAmount * CreditFeeRate,
            ReferenceNumber = $"CR-{Guid.NewGuid():N}",
            Lines = lines
        };

        _repository.AddCreditSale(sale);
        _repository.SaveChanges();

        return sale;
    }

    public CreditPayment RecordPayment(
        int creditSaleId,
        int userId,
        decimal amount,
        string paymentMethod,
        string? referenceNumber = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");

        var sale = _repository.GetById(creditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        var payments = _repository.GetPaymentsByCreditSaleId(creditSaleId);
        decimal paidAmount = payments.Sum(p => p.Amount);
        decimal outstanding = sale.TotalAmount - paidAmount;

        if (amount > outstanding)
            throw new InvalidOperationException(
                "Payment exceeds the outstanding balance.");

        var payment = new CreditPayment
        {
            CreditSaleId = creditSaleId,
            UserId = userId,
            PaymentDate = DateTime.UtcNow,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReferenceNumber = referenceNumber
        };

        _repository.AddPayment(payment);
        _repository.SaveChanges();

        return payment;
    }

    public void ModifyCreditAmount(
        int creditSaleId,
        int userId,
        decimal newPurchaseAmount)
    {
        EnsureOwner(userId);

        if (newPurchaseAmount <= 0 || newPurchaseAmount > CreditLimit)
            throw new InvalidOperationException(
                "Credit amount must be between J$1 and J$100,000.");

        var sale = _repository.GetById(creditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        decimal paidAmount = _repository
            .GetPaymentsByCreditSaleId(creditSaleId)
            .Sum(p => p.Amount);

        decimal newTotal = newPurchaseAmount * (1 + CreditFeeRate);

        if (newTotal < paidAmount)
            throw new InvalidOperationException(
                "New credit amount cannot be less than payments already made.");

        decimal oldAmount = sale.PurchaseAmount;

        sale.PurchaseAmount = newPurchaseAmount;
        sale.CreditFee = newPurchaseAmount * CreditFeeRate;

        _repository.UpdateCreditSale(sale);

        _auditRepository.Add(new AuditLogEntry
        {
            UserId = userId,
            Action = "Modified credit amount",
            EntityType = "CreditSale",
            EntityId = creditSaleId.ToString(),
            Details = $"Purchase amount changed from {oldAmount} to {newPurchaseAmount}."
        });

        _repository.SaveChanges();
    }

    public void ModifyPayment(
        int paymentId,
        int userId,
        decimal newAmount)
    {
        EnsureOwner(userId);

        if (newAmount <= 0)
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");

        var payment = _repository.GetPaymentById(paymentId);

        if (payment == null)
            throw new InvalidOperationException("Payment not found.");

        var sale = _repository.GetById(payment.CreditSaleId);

        if (sale == null)
            throw new InvalidOperationException("Credit account not found.");

        var payments = _repository
            .GetPaymentsByCreditSaleId(payment.CreditSaleId);

        decimal otherPayments = payments
            .Where(p => p.Id != paymentId)
            .Sum(p => p.Amount);

        if (otherPayments + newAmount > sale.TotalAmount)
            throw new InvalidOperationException(
                "Payment would exceed the outstanding credit amount.");

        decimal oldAmount = payment.Amount;
        payment.Amount = newAmount;

        _repository.UpdatePayment(payment);

        _auditRepository.Add(new AuditLogEntry
        {
            UserId = userId,
            Action = "Modified credit payment",
            EntityType = "CreditPayment",
            EntityId = paymentId.ToString(),
            Details = $"Payment changed from {oldAmount} to {newAmount}."
        });

        _repository.SaveChanges();
    }

    private void EnsureOwner(int userId)
    {
        var user = _repository.GetUserWithRoles(userId);

        if (user == null ||
            !user.UserRoles.Any(ur =>
                ur.Role.Name.Equals("Owner",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException(
                "Only the OWNER can modify credit records.");
        }
    }
}
