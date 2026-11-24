using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models.Credit;
using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Core.Models.Others;
using KKWholesalePOS.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class CreditRepository : ICreditRepository
{
    private readonly AppDbContext _dbContext;
    private readonly InventoryService _inventoryService;

    public CreditRepository(
        AppDbContext dbContext,
        InventoryService inventoryService)
    {
        _dbContext = dbContext ??
            throw new ArgumentNullException(nameof(dbContext));

        _inventoryService = inventoryService ??
            throw new ArgumentNullException(nameof(inventoryService));
    }

    public CreditSale? GetById(int creditSaleId)
    {
        return _dbContext.CreditSales
            .Include(sale => sale.Lines)
            .FirstOrDefault(sale => sale.Id == creditSaleId);
    }

    public List<CreditSale> GetByCustomerId(int customerId)
    {
        return _dbContext.CreditSales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .Where(sale => sale.CustomerId == customerId)
            .OrderByDescending(sale => sale.IssuedAt)
            .ToList();
    }

    public List<CreditPayment> GetPaymentsByCreditSaleId(int creditSaleId)
    {
        return _dbContext.CreditPayments
            .AsNoTracking()
            .Where(payment => payment.CreditSaleId == creditSaleId)
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.Id)
            .ToList();
    }

    public bool HasUnpaidCredit(int customerId)
    {
        var customerSales = _dbContext.CreditSales
            .AsNoTracking()
            .Where(sale => sale.CustomerId == customerId)
            .Select(sale => new
            {
                sale.Id,
                Total = sale.PurchaseAmount + sale.CreditFee
            })
            .ToList();

        if (customerSales.Count == 0)
            return false;

        var saleIds = customerSales.Select(s => s.Id).ToList();

        var paymentsBySale = _dbContext.CreditPayments
            .AsNoTracking()
            .Where(p => saleIds.Contains(p.CreditSaleId))
            .Select(p => new { p.CreditSaleId, p.Amount })
            .ToList()
            .GroupBy(p => p.CreditSaleId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        return customerSales.Any(sale =>
        {
            decimal paid = paymentsBySale.GetValueOrDefault(sale.Id, 0m);
            return sale.Total > paid;
        });
    }

    public Product? GetProductById(int productId)
    {
        return _dbContext.Products.Find(productId);
    }

    public User? GetUserWithRoles(int userId)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefault(user => user.Id == userId);
    }

    public CreditPayment? GetPaymentById(int paymentId)
    {
        return _dbContext.CreditPayments
            .FirstOrDefault(payment => payment.Id == paymentId);
    }

    public void AddCreditSale(CreditSale creditSale)
    {
        _dbContext.CreditSales.Add(creditSale);
    }

    public void UpdateCreditSale(CreditSale creditSale)
    {
        _dbContext.CreditSales.Update(creditSale);
    }

    public void AddPayment(CreditPayment payment)
    {
        _dbContext.CreditPayments.Add(payment);
    }

    public void AddPaymentWithinBalance(CreditPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.CreditSaleId <= 0 || payment.UserId <= 0)
            throw new ArgumentException("Invalid credit sale or user ID.");

        if (payment.Amount <= 0m ||
            payment.Amount != decimal.Round(payment.Amount, 2))
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero and have no more than two decimal places.");
        }

        using var transaction = _dbContext.Database.BeginTransaction(
            System.Data.IsolationLevel.Serializable);

        try
        {
            var sale = _dbContext.CreditSales
                .AsNoTracking()
                .FirstOrDefault(item => item.Id == payment.CreditSaleId);

            if (sale == null)
                throw new InvalidOperationException("Credit account not found.");

            decimal paidAmount = _dbContext.CreditPayments
                .AsNoTracking()
                .Where(existingPayment =>
                    existingPayment.CreditSaleId == payment.CreditSaleId)
                .Select(existingPayment => existingPayment.Amount)
                .AsEnumerable()
                .Sum();

            decimal outstanding = sale.PurchaseAmount + sale.CreditFee - paidAmount;

            if (outstanding <= 0m)
            {
                throw new InvalidOperationException(
                    "This credit account has already been fully paid.");
            }

            if (payment.Amount > outstanding)
            {
                throw new InvalidOperationException(
                    $"Payment exceeds the outstanding balance of J${outstanding:N2}.");
            }

            _dbContext.CreditPayments.Add(payment);
            _dbContext.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdatePayment(CreditPayment payment)
    {
        _dbContext.CreditPayments.Update(payment);
    }

    public void SaveChanges()
    {
        _dbContext.SaveChanges();
    }

    public void CreateCreditSaleWithStock(CreditSale creditSale, int userId)
    {
        ArgumentNullException.ThrowIfNull(creditSale);

        if (userId <= 0)
            throw new ArgumentException("Invalid user ID.", nameof(userId));

        if (creditSale.CustomerId <= 0)
            throw new ArgumentException(
                "Invalid customer ID.", nameof(creditSale));

        if (creditSale.Lines == null || creditSale.Lines.Count == 0)
            throw new InvalidOperationException(
                "A credit sale must contain at least one product line.");

        using var transaction = _dbContext.Database.BeginTransaction();

        try
        {
            if (HasUnpaidCredit(creditSale.CustomerId))
            {
                throw new InvalidOperationException(
                    "Customer already has an unpaid credit account.");
            }

            var requestedQuantities = creditSale.Lines
                .GroupBy(line => line.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(line => line.Quantity));

            foreach (var requested in requestedQuantities)
            {
                if (requested.Key <= 0 || requested.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Credit sale contains an invalid product or quantity.");
                }

                var product = _dbContext.Products
                    .FirstOrDefault(item => item.Id == requested.Key);

                if (product == null)
                {
                    throw new InvalidOperationException(
                        $"Product with ID {requested.Key} was not found.");
                }

                if (requested.Value > product.StockQuantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for {product.Name}. " +
                        $"Available: {product.StockQuantity}; " +
                        $"requested: {requested.Value}.");
                }
            }

            _dbContext.CreditSales.Add(creditSale);
            _dbContext.SaveChanges();

            foreach (var line in creditSale.Lines)
            {
                _inventoryService.ApplyStockChange(
                    line.ProductId,
                    -line.Quantity,
                    userId,
                    InventoryService.CreditSale,
                    $"Credit sale {creditSale.ReferenceNumber}");
            }

            _dbContext.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}