namespace GoldSystem.Data.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int BillId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public DateOnly PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Bill Bill { get; set; } = null!;
}
