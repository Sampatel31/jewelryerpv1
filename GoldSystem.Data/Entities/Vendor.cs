namespace GoldSystem.Data.Entities;

public class Vendor
{
    public int VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
