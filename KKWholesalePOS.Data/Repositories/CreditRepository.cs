//created 22.1126 @1:14
//did not see any, i dont if need as yes

//connects the credit service to the database
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
        _dbContext = dbContext;
        _inventoryService = inventoryService;
    }

    public CreditSale? GetById(int creditSaleId)
    {
        return _dbContext.CreditSales
            .Include(s => s.Lines)
            .FirstOrDefault(s => s.Id == creditSaleId);
    }

    public List<CreditSale> GetByCustomerId(int customerId)
    {
        return _dbContext.CreditSales
            .Include(s => s.Lines)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.IssuedAt)
            .ToList();
    }

    public List<CreditPayment> GetPaymentsByCreditSaleId(int creditSaleId)
    {
        return _dbContext.CreditPayments
            .AsNoTracking()
            .Where(p => p.CreditSaleId == creditSaleId)
            .OrderBy(p => p.PaymentDate)
            .ToList();
    }

    public bool HasUnpaidCredit(int customerId)
    {
        return _dbContext.CreditSales.Any(s =>
            s.CustomerId == customerId &&
            s.PurchaseAmount + s.CreditFee >
            (_dbContext.CreditPayments
                .Where(p => p.CreditSaleId == s.Id)
                .Sum(p => (decimal?)p.Amount) ?? 0m));
    }

    public Product? GetProductById(int productId)
    {
        return _dbContext.Products.Find(productId);
    }

    public User? GetUserWithRoles(int userId)
    {
        return _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefault(u => u.Id == userId);
    }

    public CreditPayment? GetPaymentById(int paymentId)
    {
        return _dbContext.CreditPayments
            .FirstOrDefault(p => p.Id == paymentId);
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
    using var transaction = _dbContext.Database.BeginTransaction();

    try
    {
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
