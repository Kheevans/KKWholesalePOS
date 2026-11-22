
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Data.Repositories;

public class RegularSaleRepository : IRegularSaleRepository
{
    private readonly AppDbContext _dbContext;

    public RegularSaleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Product? FindProduct(string barcode)
    {
        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Barcode == barcode);
    }

    public Product? FindProductById(int productId)
    {
        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Id == productId);
    }

    public RegularSale SaveCashSale(
        IReadOnlyList<SaleRequestLine> items,
        decimal cashReceived,
        int cashierId,
        string cashierName)
    {
        return SaveSale(items, cashierId, cashierName, sale =>
        {
            if (sale.Total <= 0m || cashReceived < sale.Total)
            {
                throw new ArgumentException(
                    "Cash payment is less than the total.");
            }

            if (decimal.Round(cashReceived, 2) != cashReceived)
            {
                throw new ArgumentException(
                    "Cash amount must have at most two decimal places.");
            }

            sale.PaymentMethod = "Cash";
            sale.CashReceived = cashReceived;
            sale.ChangeDue = cashReceived - sale.Total;
        });
    }

    public RegularSale SaveCardSale(
        IReadOnlyList<SaleRequestLine> items,
        string cardType,
        string cardLastFour,
        string approvalCode,
        int cashierId,
        string cashierName)
    {
        if (string.IsNullOrWhiteSpace(cardType) ||
            string.IsNullOrWhiteSpace(cardLastFour) ||
            string.IsNullOrWhiteSpace(approvalCode))
        {
            throw new ArgumentException("Card details are required.");
        }

        return SaveSale(items, cashierId, cashierName, sale =>
        {
            if (sale.Total <= 0m)
            {
                throw new ArgumentException(
                    "Sale total must be greater than zero.");
            }

            sale.PaymentMethod = "Card";
            sale.CashReceived = sale.Total;
            sale.ChangeDue = 0m;
            sale.CardType = cardType;
            sale.CardLastFour = cardLastFour;
            sale.ApprovalCode = approvalCode;
        });
    }

    private RegularSale SaveSale(
        IReadOnlyList<SaleRequestLine> items,
        int cashierId,
        string cashierName,
        Action<RegularSale> applyPayment)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Please add at least one item.");
        }

        if (cashierId <= 0 || string.IsNullOrWhiteSpace(cashierName))
        {
            throw new ArgumentException("A valid cashier is required.");
        }

        using var transaction = _dbContext.Database.BeginTransaction();

        try
        {
            var sale = new RegularSale
            {
                CashierId = cashierId,
                CashierName = cashierName,
                CompletedAt = DateTime.UtcNow
            };

            decimal total = 0m;

            foreach (var group in items.GroupBy(item => item.ProductId))
            {
                int quantity = 0;

                foreach (SaleRequestLine item in group)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new ArgumentException(
                            "Quantity must be greater than zero.");
                    }

                    quantity = checked(quantity + item.Quantity);
                }

                var product = _dbContext.Products
                    .AsNoTracking()
                    .FirstOrDefault(p => p.Id == group.Key);

                if (product == null)
                {
                    throw new InvalidOperationException("Product not found.");
                }

                if (product.SellingPrice < 0m)
                {
                    throw new InvalidOperationException(
                        "The stored selling price is invalid.");
                }

                int affectedRows = _dbContext.Database.ExecuteSqlInterpolated(
                    $"UPDATE Products SET StockQuantity = StockQuantity - {quantity} WHERE Id = {product.Id} AND StockQuantity >= {quantity}");

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException(
                        "Not enough stock for " + product.Name + ".");
                }

                decimal lineTotal = Math.Round(
                    product.SellingPrice * quantity,
                    2,
                    MidpointRounding.AwayFromZero);

                total += lineTotal;

                sale.Lines.Add(new RegularSaleLine
                {
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    ProductName = product.Name,
                    UnitPrice = product.SellingPrice,
                    Quantity = quantity,
                    LineTotal = lineTotal
                });
            }

            sale.Total = Math.Round(
                total, 2, MidpointRounding.AwayFromZero);

            applyPayment(sale);

            _dbContext.RegularSales.Add(sale);
            _dbContext.SaveChanges();

            foreach (RegularSaleLine line in sale.Lines)
            {
                _dbContext.StockMovements.Add(new StockMovement
                {
                    RegularSaleId = sale.Id,
                    ProductId = line.ProductId,
                    QuantityChange = -line.Quantity,
                    MovementType = "Regular sale",
                    UserId = cashierId,
                    RecordedAt = sale.CompletedAt
                });
            }

            _dbContext.SaveChanges();
            transaction.Commit();

            return sale;
        }
        catch
        {
            transaction.Rollback();
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
