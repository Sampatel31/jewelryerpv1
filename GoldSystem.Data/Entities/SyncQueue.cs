namespace GoldSystem.Data.Entities;

public class SyncQueue
{
    public long QueueId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation properties
    public Branch Branch { get; set; } = null!;
}
