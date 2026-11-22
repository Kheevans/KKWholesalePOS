
using KKWholesalePOS.Core.Models;

namespace KKWholesalePOS.Core.Interfaces;

public interface ICreditRepository
{
    CreditSale? GetById(int creditSaleId);
//created for use in creditrepository
    Product? GetProductById(int productId);
    User? GetUserWithRoles(int userId);
    CreditPayment? GetPaymentById(int paymentId);

    // Retrieve all credit sales belonging to a customer.
    List<CreditSale> GetByCustomerId(int customerId);

    // Retrieve the payments recorded for a credit sale.
    List<CreditPayment> GetPaymentsByCreditSaleId(int creditSaleId);

    // Check whether a customer has an unpaid credit sale.
    bool HasUnpaidCredit(int customerId);

    // Save a new credit sale.
    void AddCreditSale(CreditSale creditSale);

    // Update an existing credit sale.
    void UpdateCreditSale(CreditSale creditSale);

    void AddPayment(CreditPayment payment);

    // Update a recorded payment.
    void UpdatePayment(CreditPayment payment);

    // Save database changes.
    void SaveChanges();
}
