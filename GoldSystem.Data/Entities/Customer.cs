namespace GoldSystem.Data.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal TotalPurchased { get; set; }
    public decimal CreditLimit { get; set; }
    public int BranchId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
