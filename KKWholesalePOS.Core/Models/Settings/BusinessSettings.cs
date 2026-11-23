namespace KKWholesalePOS.Core.Models.Settings;

public class BusinessSettings
{
    public int Id { get; set; }

    // Business information
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;

    // Receipt settings
    public string ReceiptHeader { get; set; } = string.Empty;
    public string ReceiptFooter { get; set; } = string.Empty;
    public bool ShowBusinessContactDetails { get; set; } = true;

    // System preferences
    public string CurrencyCode { get; set; } = "JMD";
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    // Inventory preferences
    public int DefaultMinimumStockLevel { get; set; } = 0;
}