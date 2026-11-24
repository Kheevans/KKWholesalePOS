using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Inventory;

namespace KKWholesalePOS.Core.Services;

public class InventoryManagementService
{
    private readonly IInventoryRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public InventoryManagementService(
        IInventoryRepository repository,
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

    public List<Product> GetAllProducts()
    {
        RequirePermission("Inventory.View");
        return _repository.GetAllProducts();
    }

    public Product? GetProductById(int productId)
    {
        RequirePermission("Inventory.View");

        if (productId <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(productId));

        return _repository.GetProductById(productId);
    }

    public Product? GetProductByBarcode(string barcode)
    {
        RequirePermission("Inventory.View");

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));

        return _repository.GetProductByBarcode(barcode.Trim());
    }

    public List<Product> SearchProducts(string query)
    {
        RequirePermission("Inventory.View");

        if (string.IsNullOrWhiteSpace(query))
            return _repository.GetAllProducts();

        return _repository.SearchProducts(query);
    }

    public List<Product> GetLowStockProducts()
    {
        RequirePermission("Inventory.View");
        return _repository.GetLowStockProducts();
    }

    public List<Product> GetNegativeStockProducts()
    {
        RequirePermission("Inventory.View");
        return _repository.GetNegativeStockProducts();
    }

    public List<StockMovement> GetStockMovements(int? productId = null)
    {
        RequirePermission("Inventory.View");

        if (productId.HasValue && productId.Value <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(productId));

        return _repository.GetStockMovements(productId);
    }

    public void SetMinimumStockLevel(int productId, int minimumStockLevel)
    {
        int userId = RequirePermission("Inventory.Adjust");

        if (productId <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(productId));

        if (minimumStockLevel < 0)
            throw new ArgumentException(
                "Minimum stock level cannot be negative.",
                nameof(minimumStockLevel));

        var product = _repository.GetProductById(productId)
            ?? throw new InvalidOperationException("Product not found.");

        int oldMinimum = product.MinimumStockLevel;
        _repository.UpdateMinimumStockLevel(productId, minimumStockLevel);

        _auditLogService.LogAction(
            userId,
            "MinimumStockLevelUpdated",
            "Product",
            productId.ToString(),
            $"Minimum stock level for '{product.Name}' changed from {oldMinimum} to {minimumStockLevel}.");
    }

    public async Task ReceiveStockAsync(
        int productId,
        int quantity,
        string? notes = null)
    {
        int userId = RequirePermission("Inventory.Receive");

        if (productId <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException(
                "Received quantity must be greater than zero.",
                nameof(quantity));

        await _repository.ReceiveStockAsync(
            productId,
            quantity,
            userId,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());

        _auditLogService.LogAction(
            userId,
            "StockReceived",
            "Product",
            productId.ToString(),
            $"Received {quantity} unit(s) for Product ID {productId}." +
            (string.IsNullOrWhiteSpace(notes) ? string.Empty : $" Notes: {notes.Trim()}"));
    }

    public async Task AdjustStockAsync(
        int productId,
        int quantityChange,
        string reason)
    {
        int userId = RequirePermission("Inventory.Adjust");

        if (productId <= 0)
            throw new ArgumentException("Invalid product ID.", nameof(productId));

        if (quantityChange == 0)
            throw new ArgumentException(
                "Adjustment quantity cannot be zero.",
                nameof(quantityChange));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "An adjustment reason is required.",
                nameof(reason));

        string trimmedReason = reason.Trim();

        await _repository.AdjustStockAsync(
            productId,
            quantityChange,
            userId,
            trimmedReason);

        _auditLogService.LogAction(
            userId,
            "StockAdjusted",
            "Product",
            productId.ToString(),
            $"Adjusted stock for Product ID {productId} by {quantityChange:+#;-#;0} unit(s). Reason: {trimmedReason}");
    }
}