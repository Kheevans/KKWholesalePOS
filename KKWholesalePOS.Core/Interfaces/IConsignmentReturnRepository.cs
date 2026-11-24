using KKWholesalePOS.Core.Models.Consignment;

namespace KKWholesalePOS.Core.Interfaces;

public interface IConsignmentReturnRepository
{
    Consignment? FindConsignmentByReference(string referenceNumber); 

    IReadOnlyList<ConsignmentReturn> GetReturns(int consignmentId);

    IReadOnlyList<ConsignmentPayment> GetPayments(int consignmentId);

    ConsignmentReturn SaveReturn(ConsignmentReturn consignmentReturn);

    ConsignmentPayment SavePayment(ConsignmentPayment payment);
}