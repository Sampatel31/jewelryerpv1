namespace GoldSystem.Sync.Models;

public record SyncPayload(
    string TableName,
    int RecordId,
    string Operation,
    Dictionary<string, object> Data,
    DateTime CreatedAt);

public record SyncBatch(
    int BranchId,
    List<SyncPayload> Records,
    int Count);

public record SyncAck(
    int SyncedCount,
    int FailedCount,
    List<SyncError> Errors,
    DateTime SyncedAt);

public record SyncError(
    long QueueId,
    string TableName,
    int RecordId,
    string ErrorMessage);

public record ConflictResolutionResult(
    bool HasConflict,
    object? Winner,
    string Resolution);
