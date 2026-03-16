namespace GoldSystem.Data.Entities;

public class Branch
{
    public int BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsOwnerBranch { get; set; }
    public string SqlConnectionString { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<GoldRate> GoldRates { get; set; } = new List<GoldRate>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<SyncQueue> SyncQueues { get; set; } = new List<SyncQueue>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
