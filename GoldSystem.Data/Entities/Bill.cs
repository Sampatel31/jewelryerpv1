namespace GoldSystem.Data.Entities;

public class Bill
{
    public int BillId { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public DateOnly BillDate { get; set; }
    public int CustomerId { get; set; }
    public decimal RateSnapshot22K { get; set; }
    public decimal RateSnapshot24K { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingAmount { get; set; }
    public decimal WastageAmount { get; set; }
    public decimal StoneCharge { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal ExchangeValue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public int UserId { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    public ICollection<OldGoldExchange> OldGoldExchanges { get; set; } = new List<OldGoldExchange>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Item> SoldItems { get; set; } = new List<Item>();
}
