namespace GoldSystem.Data.Entities;

public class Item
{
    public int ItemId { get; set; }
    public string? HUID { get; set; }
    public string TagNo { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Purity { get; set; } = string.Empty;
    public decimal GrossWeight { get; set; }
    public decimal StoneWeight { get; set; }
    public decimal NetWeight { get; set; }
    public decimal PureGoldWeight { get; set; }
    public string MakingType { get; set; } = string.Empty;
    public decimal MakingValue { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal PurchaseRate24K { get; set; }
    public decimal CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public int VendorId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public int? SoldBillId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Category Category { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public Bill? SoldBill { get; set; }
    public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
}
