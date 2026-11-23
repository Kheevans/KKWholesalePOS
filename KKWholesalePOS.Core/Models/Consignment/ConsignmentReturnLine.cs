namespace KKWholesalePOS.Core.Models.Consignment;

public class ConsignmentReturnLine
{
    public int Id { get; set; }

    public int ConsignmentReturnId { get; set; }

    public int ConsignmentLineId { get; set; }

    public int QuantityReturned { get; set; }

    public int QuantityAccepted { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal AcceptedValue => QuantityAccepted * UnitPrice;

    public int QuantityRejected => QuantityReturned - QuantityAccepted;
}