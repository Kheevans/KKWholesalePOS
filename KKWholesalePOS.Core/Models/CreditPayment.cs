namespace KKWholesalePOS.Core.Models;

public class CreditPayment
{
    public int Id {get; set;}

//invoice this payment belongs to
    public int CreditTransactionId{ get; set;}

    public CreditTransaction CreditTransaction {get; set;} =null!;

//amount paid by customer
    public decimal AmountPaid{get; set;}

//date the payment was made 
    public DateTime PaymentDate {get; set;} = DateTime.UtcNow;

    public int RecordedByUserId{ get; set;}

    public User RecordedByUser{get; set;} = null!;
}