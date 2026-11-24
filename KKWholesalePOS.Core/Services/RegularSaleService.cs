
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Sales;
using System.Text;

namespace KKWholesalePOS.Core.Services;

public class RegularSaleService
{
    public static readonly string[] CardTypes =
    {
        "Visa",
        "Mastercard",
        "Debit (Keycard)",
        "American Express"
    };

    private readonly IRegularSaleRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public RegularSaleService(
        IRegularSaleRepository repository,
        UserSessionService userSession,
        AuthorizationService authorizationService,
        AuditLogService auditLogService)
    {
        _repository = repository;
        _userSession = userSession;
        _authorizationService = authorizationService;
        _auditLogService = auditLogService;
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

    public Product? FindProduct(string barcode)
    {
        RequirePermission("Sales.Process");

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Barcode is required.");
        }

        return _repository.FindProduct(barcode.Trim());
    }

    public Product? FindProductById(int productId)
    {
        RequirePermission("Sales.Process");

        if (productId <= 0)
        {
            throw new ArgumentException("A valid product is required.");
        }

        return _repository.FindProductById(productId);
    }

    public RegularSale CompleteCashSale(
        IReadOnlyList<SaleRequestLine> items,
        decimal cashReceived,
        int cashierId,
        string cashierName)
    {
        RequirePermission("Sales.Process");

        ValidateSale(items, cashierId, cashierName);

        if (cashReceived <= 0m)
        {
            throw new ArgumentException(
                "Cash received must be greater than zero.");
        }

        var sale = _repository.SaveCashSale(
            items, cashReceived, cashierId, cashierName);

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id ?? cashierId,
            "RegularSaleCompleted",
            "RegularSale",
            sale.Id.ToString(),
            $"Cash sale completed. Total: J${sale.Total:N2}, Cash: J${cashReceived:N2}, Change: J${sale.ChangeDue:N2}.");

        return sale;
    }

    public RegularSale CompleteCardSale(
        IReadOnlyList<SaleRequestLine> items,
        string cardType,
        string cardLastFour,
        string approvalCode,
        int cashierId,
        string cashierName)
    {
        RequirePermission("Sales.Process");

        ValidateSale(items, cashierId, cashierName);

        if (string.IsNullOrWhiteSpace(cardType) ||
            string.IsNullOrWhiteSpace(cardLastFour) ||
            string.IsNullOrWhiteSpace(approvalCode))
        {
            throw new ArgumentException(
                "Complete card payment details are required.");
        }

        if (cardLastFour.Length != 4 ||
            !cardLastFour.All(char.IsDigit))
        {
            throw new ArgumentException(
                "Card last four digits must contain exactly four digits.");
        }

        var sale = _repository.SaveCardSale(
            items, cardType, cardLastFour, approvalCode, cashierId, cashierName);

        _auditLogService.LogAction(
            _userSession.CurrentUser?.Id ?? cashierId,
            "RegularSaleCompleted",
            "RegularSale",
            sale.Id.ToString(),
            $"Card sale ({cardType} ****{cardLastFour}) completed. Total: J${sale.Total:N2}, Approval: {approvalCode}.");

        return sale;
    }

    private static void ValidateSale(
        IReadOnlyList<SaleRequestLine> items,
        int cashierId,
        string cashierName)
    {
        if (items == null || items.Count == 0)
        {
            throw new ArgumentException(
                "A sale must contain at least one item.");
        }

        if (cashierId <= 0)
        {
            throw new ArgumentException("A valid cashier is required.");
        }

        if (string.IsNullOrWhiteSpace(cashierName))
        {
            throw new ArgumentException("Cashier name is required.");
        }

        if (items.Any(item => item == null ||
                              item.ProductId <= 0 ||
                              item.Quantity <= 0))
        {
            throw new ArgumentException(
                "All sale items must have a valid product and quantity.");
        }
    }

    public void CheckQuantity(
        int productId,
        int quantity,
        int alreadyInCart = 0)
    {
        RequirePermission("Sales.Process");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (alreadyInCart < 0)
            throw new ArgumentException("Invalid cart quantity.");

        var product = _repository.FindProductById(productId);

        if (product == null)
            throw new InvalidOperationException("Product not found.");

        if (quantity > product.StockQuantity - alreadyInCart)
            throw new InvalidOperationException(
                "Not enough stock for " + product.Name +
                ". Available: " + product.StockQuantity + ".");
    }

    public decimal CalculateTotal(IReadOnlyList<SaleRequestLine> items)
    {
        RequirePermission("Sales.Process");

        decimal total = 0m;

        foreach (var group in items.GroupBy(i => i.ProductId))
        {
            int quantity = 0;

            foreach (var item in group)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException(
                        "Quantity must be greater than zero.");

                quantity = checked(quantity + item.Quantity);
            }

            CheckQuantity(group.Key, quantity);

            var product = _repository.FindProductById(group.Key);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            decimal lineTotal = Math.Round(
                product.SellingPrice * quantity,
                2,
                MidpointRounding.AwayFromZero);

            total += lineTotal;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static string MakeReceipt(RegularSale sale)
    {
        var receipt = new StringBuilder();

        receipt.AppendLine("K&K WHOLESALE");
        receipt.AppendLine("REGULAR SALE RECEIPT");
        receipt.AppendLine("Sale #: " + sale.Id);
        receipt.AppendLine(
            "Date: " + sale.CompletedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        receipt.AppendLine("Cashier: " + sale.CashierName);
        receipt.AppendLine("--------------------------------");

        foreach (var line in sale.Lines)
        {
            receipt.AppendLine(line.ProductName);
            receipt.AppendLine(
                $"  {line.Quantity} x J${line.UnitPrice:N2} = J${line.LineTotal:N2}");
        }

        receipt.AppendLine("--------------------------------");
        receipt.AppendLine($"TOTAL: J${sale.Total:N2}");

        if (sale.PaymentMethod == "Card")
        {
            receipt.AppendLine($"CARD ({sale.CardType}): J${sale.CashReceived:N2}");
            receipt.AppendLine("Card No: **** **** **** " + sale.CardLastFour);
            receipt.AppendLine("Approval Code: " + sale.ApprovalCode);
        }
        else
        {
            receipt.AppendLine($"CASH: J${sale.CashReceived:N2}");
            receipt.AppendLine($"CHANGE: J${sale.ChangeDue:N2}");
        }

        receipt.AppendLine("Thank you for shopping with us!");

        return receipt.ToString();
    }
}
