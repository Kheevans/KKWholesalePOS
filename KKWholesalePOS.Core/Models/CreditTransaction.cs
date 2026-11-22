//for customer's credit purchase and invoice

using System.Data;

namespace KKWholesalePOS.Core.Models;

public class CreditTransaction
{
    public int Id {get; set;}

// identify the customer who made the credit purchase
    public int CustomerId{get; set; }

    public Customer Customer {get; set;} = null!;

    public int ProductId{ get; set;}

    public Product Product{ get; set;} =null!;

    public int Quantity{get; set;}

//store purchase amount before credit fee
    public decimal PurchaseAmount {get; set;}

//store 5% credit fee
    public decimal CreditFee {get; set;}
//purchase amount + credit fee
    public decimal TotalAmount{get; set;}

// Date the credit purchase was maded/recorded
    public DateTime TransactionDate {get; set; } = DateTime.UtcNow;

//Due date for customer's credit; date customer must pay the invoice
    public DataSetDateTime DueDate {get; set;}

    public ICollection<CreditPayment> Payments{get; set;}
    = new List<CreditPayment>();

}