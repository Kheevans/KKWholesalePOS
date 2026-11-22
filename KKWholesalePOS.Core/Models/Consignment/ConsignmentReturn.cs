namespace KKWholesalePOS.Core.Models.Consignment;

public class ConsignmentReturn
{
    public int Id { get; set; }

    public int ConsignmentId { get; set; }

    public int UserId { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public string ReferenceNumber { get; set; } = string.Empty;

    public List<ConsignmentReturnLine> Lines { get; set; } = new();
}
