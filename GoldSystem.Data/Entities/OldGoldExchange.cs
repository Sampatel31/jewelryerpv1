namespace GoldSystem.Data.Entities;

public class OldGoldExchange
{
    public int ExchangeId { get; set; }
    public int BillId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal GrossWeight { get; set; }
    public string TestPurity { get; set; } = string.Empty;
    public decimal ExchangeRateApplied { get; set; }
    public decimal ExchangeValue { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Bill Bill { get; set; } = null!;
}
