namespace GoldSystem.Data.Entities;

public class GoldRate
{
    public int RateId { get; set; }
    public DateOnly RateDate { get; set; }
    public TimeOnly RateTime { get; set; }
    public decimal Rate24K { get; set; }
    public decimal Rate22K { get; set; }
    public decimal Rate18K { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsManualOverride { get; set; }
    public string? OverrideNote { get; set; }
    public int BranchId { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}
