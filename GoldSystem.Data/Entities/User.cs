namespace GoldSystem.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public ICollection<GoldRate> GoldRates { get; set; } = new List<GoldRate>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
