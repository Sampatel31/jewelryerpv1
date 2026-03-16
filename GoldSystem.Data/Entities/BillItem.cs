namespace GoldSystem.Data.Entities;

public class BillItem
{
    public int BillItemId { get; set; }
    public int BillId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Purity { get; set; } = string.Empty;
    public decimal GrossWeight { get; set; }
    public decimal StoneWeight { get; set; }
    public decimal NetWeight { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal WastageWeight { get; set; }
    public decimal BillableWeight { get; set; }
    public decimal PureGoldWeight { get; set; }
    public decimal RateUsed24K { get; set; }
    public decimal GoldValue { get; set; }
    public string MakingType { get; set; } = string.Empty;
    public decimal MakingValue { get; set; }
    public decimal MakingAmount { get; set; }
    public decimal StoneCharge { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGST_Amount { get; set; }
    public decimal SGST_Amount { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation properties
    public Bill Bill { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
