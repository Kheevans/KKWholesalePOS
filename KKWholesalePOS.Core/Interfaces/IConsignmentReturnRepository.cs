using KKWholesalePOS.Core.Models.Consignment;

namespace KKWholesalePOS.Core.Interfaces;

public interface IConsignmentReturnRepository
{
    // Must include Lines
    Consignment? FindConsignmentByReference(string referenceNumber);

    // Must include Lines
    IReadOnlyList<ConsignmentReturn> GetReturns(int consignmentId);

    IReadOnlyList<ConsignmentPayment> GetPayments(int consignmentId);

    // Implementation must, in ONE database transaction: save the return,
    // then call InventoryService.ApplyStockChange for every line with
    // QuantityAccepted > 0 (movement type ConsignmentReturn).
    ConsignmentReturn SaveReturn(ConsignmentReturn consignmentReturn);

    ConsignmentPayment SavePayment(ConsignmentPayment payment);
}