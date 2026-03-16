namespace GoldSystem.Data.Entities;

public class AuditLog
{
    public long LogId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public int BranchId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
