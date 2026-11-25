using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Others;

namespace KKWholesalePOS.Core.Services;

public class CustomerManagementService
{
    private readonly ICustomerRepository _repository;
    private readonly UserSessionService _userSession;
    private readonly AuthorizationService _authorizationService;
    private readonly AuditLogService _auditLogService;

    public CustomerManagementService(
        ICustomerRepository repository,
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
        if (currentUser == null || !_authorizationService.HasPermission(currentUser, permissionName))
        {
            throw new UnauthorizedAccessException("You do not have permission to perform this action.");
        }

        return currentUser.Id;
    }

    public List<Customer> GetCustomers(string? searchTerm = null, bool includeInactive = false)
    {
        RequirePermission("Customers.Manage");
        return _repository.Search(searchTerm, includeInactive);
    }

    public Customer AddCustomer(string name, string? phoneNumber, string? address)
    {
        int userId = RequirePermission("Customers.Manage");

        string trimmedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("Customer name is required.", nameof(name));

        var customer = new Customer
        {
            Name = trimmedName,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _repository.Add(customer);
        _repository.SaveChanges();

        _auditLogService.LogAction(
            userId,
            "CustomerCreated",
            "Customer",
            customer.Id.ToString(),
            $"Created customer '{customer.Name}'.");

        return customer;
    }

    public void UpdateCustomer(int customerId, string name, string? phoneNumber, string? address, bool isActive)
    {
        int userId = RequirePermission("Customers.Manage");

        if (customerId <= 0)
            throw new ArgumentException("Invalid customer ID.", nameof(customerId));

        string trimmedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ArgumentException("Customer name is required.", nameof(name));

        var existing = _repository.GetById(customerId)
            ?? throw new InvalidOperationException("Customer not found.");

        existing.Name = trimmedName;
        existing.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        existing.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        existing.IsActive = isActive;

        _repository.Update(existing);
        _repository.SaveChanges();

        _auditLogService.LogAction(
            userId,
            "CustomerUpdated",
            "Customer",
            customerId.ToString(),
            $"Updated customer '{existing.Name}' (Active: {existing.IsActive}).");
    }
}