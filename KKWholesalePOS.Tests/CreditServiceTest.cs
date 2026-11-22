using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using KKWholesalePOS.Core.Interfaces;
using KKWholesalePOS.Core.Models;
using KKWholesalePOS.Core.Services;

namespace KKWholesalePOS.Tests;

public class CreditServiceTests
{
    
    private void ValidatePurchaseQuantity(Product product, int quantity)
    {
        throw new NotImplementedException();
    }

    private decimal CalculateCreditTotal(decimal purchaseAmount)
    {
        throw new NotImplementedException();
    }    }


    [Fact]
    //calculates credit total and the added 5 percent
    //orginal amount is $10,000 and the expected total is $10, 500
    public void CalculateCreditTotal_FivePercent()
    {
        decimal purchaseAmount = 10000m;
        decimal expectedTotal = 10500m;

        var service = new CreditServiceTests();
        decimal actualTotal = service.CalculateCreditTotal(purchaseAmount);

        Assert.Equal(expectedTotal, actualTotal);
    }

    
//TS-02.2A & 2.2B
    [Fact]
    //input of 0 and gives an argument exception 
    public void CreateCreditPurchase_ZeroQuantity()
    {
        var service = new CreditServiceTests();
        var product = new Product
        {
            Id = 1,
            Name = "Boom",
            SellingPrice = 1000m,
            StockQuantity = 20,
            UnitsPerCase = 1
        };
        int quantity = 0;

        Assert.Throws<ArgumentException>(() =>
        service.ValidatePurchaseQuantity(product, quantity));
    }

    [Fact]
    //input of -2 and gives argumentexception
    public void CreatedCreditPurchase_NegativeQuanitity()
    {
        var service = new CreditServiceTests();
        var product = new Product
        {
            Id = 1,
            Name = "Boom",
            SellingPrice = 1000m,
            StockQuantity = 20,
            UnitsPerCase = 1
        };

        int quantity = -2;

        Assert.Throws<ArgumentException>(() =>
        service.ValidatePurchaseQuantity(product, quantity));
    }

    [Fact]
    // input of 25 while stock is 20 gives and invalid operation exception
        public void CreateCreditPurchase_ExccedsStock()
        {
            var service = new CreditServiceTests();
            var product = new Product
            {
                Id = 1,
                Name = "Boom",
                StockQuantity = 20,
                UnitsPerCase = 1,
            };
            int quantity = 25;

            Assert.Throws<InvalidOperationException>(() =>
            service.ValidatePurchaseQuantity(product, quantity));

    }
//TS-0.27A & 7B

    [Fact]
    //test when the credit limit is met; accepted
    public void CreateCreditPurchase_WithinCreditLimi()
    {
        var service = new CreditServiceTests();
        decimal purchaseAmount = 100000m;
        bool result = service.IsWithinCreditLimit(purchaseAmount);
        Assert.True(result);
    }

    [Fact]

    //test when purchase exceeds the credit limit; rejection
    public void CreateCreditPurchase_ExceedsCreditLimit()
    {
        var service = CreditServiceTests();
        decimal purchaseAmount = 100001m;
        bool result = service.IsWithinCreditLimit(purchaseAmount);
        Assert.False(result);
    }

    [Fact]

    //test when customer has no unpadin credit and tries to request on; accept
    public void CreateCreditPurchase_NoUnpaidInvoice()
    {
        var service = new CreditServiceTests();
        var customer = new Customer
        {
            Id = 1,
            Name = "Macy Barr"
        };

        bool result = service.CanCreateCreditPurchase(
            customer,
            hasUnpaidInvoice);

        Assert.True(result);
    }

    [Fact]
    //test when customer has an unpaid creadit and tries to request another; reject
    public void CreateCreatePurchase_WithUnpaidInvoice()
    {
        var service = new CreditServiceTests();
        var customer = new Customer
        {
            Id = 1,
            Name= "Macy Barr"
        };
        bool hasUnpaidInvoice = true;

        bool result = service.CanCreateCreditPurchase(
            customer,
            hasUnpaidInvoice);

        Assert.False(result);   
    }

    //TS-02.4A & 2.4B
    [Fact]
    //test that payment on an outstanding balance leaves the correct difference
    public void RecordPayment_PartialPayment()
    {
        var service = new CreditServiceTests();

        decimal originalBalance = 10500m;
        decimal paymentAnount = 3000m;
        decimal expectedBalance = 7500m;

        decimal actualBalance = service.CalculateOutstandingBalance(
            originalBalance,
            paymentAnount);

        Assert.Equal(expectedBalance, actualBalance);
    }

    [Fact]
    //test that payment greater than the outstanding balance is rejected
    public void RecordPayment_ExceedsOutstandingBalance()
    {
        var service = new CreditServiceTests();

        decimal outstandingBalance = 10500m;
        decimal paymentAmount = 11000m;

        Assert.Throws<InvalidOperationException>(() =>
            service.ValidatePayment(
                outstandingBalance,
                paymentAmount));
    }

    //TS 02.6A & 2.6B
    [Fact]
    //test that credit due date is 14 days after the original data
    public void CreateCreditPurchase_ShouldSetDueDate()
    {
        var service = new CreditServoce();

        DateTime purchase = new DateTime(2026, 11, 22);
        DateTime expectedDueDate = new DateTime(2026, 12, 6);

        DateTime actualDueDate = service.CalculateDueDate(purchaseDate);

        Assert.Equal(expectedDueDate, actualDueDate);
    }

    [Fact]
    //test that credit due date is not calculated if the deadline is missing
    public void CreateCreditPurchase_NoValidDeadline()
    {
        var service = new CreditServiceTests(null);

        DateTime purchaseDate = new DateTime(2026,11,22);

        DateTime? actualDueDate = service.CalculateDueDate(purchaseDate);

        Assert.Null(actualDueDate);
    }

//TS02.1aA & 2.1B
[Fact]
//test that staff can retrieve customer's detail
public void GetCustomerCreditAccount_ExistingCustomer_ShouldReturnAccountDetails()
{
    // Arrange
    var service = new CreditService();

    var customer = new Customer
    {
        Id = 1,
        Name = "Macy Barr",
        PhoneNumber = "8763459830"
    };

    decimal expectedBalance = 7500m;

    var result = service.GetCustomerCreditAccount(customer.Id);

    Assert.NotNull(result);
    Assert.Equal(customer.Name, result.Customer.Name);
    Assert.Equal(expectedBalance, result.OutstandingBalance);
    Assert.NotNull(result.Transactions);
}


[Fact]
//test that a non-existing customer returns no account
public void GetCustomerCreditAccount_NonexistentCustomer_ShouldReturnNull()
{
    var service = new CreditService();

    int nonexistentCustomerId = 999;

    var result = service.GetCustomerCreditAccount(
        nonexistentCustomerId);

    Assert.Null(result);
}

//TS02.5A & 2.5B
[Fact]
//test that the owner can modify a credit record
public void ModifyCreditRecord_Owner()
    {
        var service = new CreditServiceTests();
        var owner = new User
        {
            Id = 1,
            Username = "Owner",
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role
                    {
                        Name = "Owner"
                    }
            }   }
        
    };




[Fact]
//test that an ordinay staff cannot modify a credit record
public void ModifyCreditRecord_Staff_ShouldRejectModification()
{
    var service = new CreditService();

    var staff = new User
    {
        Id = 2,
        Username = "staff",
        UserRoles = new List<UserRole>
        {
            new UserRole
            {
                Role = new Role
                {
                    Name = "Staff"
                }
            }
        }
    };

    
}


