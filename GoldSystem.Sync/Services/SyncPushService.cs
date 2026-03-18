using System.Text.RegularExpressions;
using GoldSystem.Data;
using GoldSystem.Sync.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GoldSystem.Sync.Services;

/// <summary>
/// Pushes pending SyncQueue records from a branch database to the owner (head-office) database.
/// </summary>
public interface ISyncPushService
{
    Task<SyncAck> PushPendingSyncsAsync(int branchId, int maxRecords = 500);
}

/// <summary>
/// Default implementation of <see cref="ISyncPushService"/>.
/// Connects directly to the owner SQL Server and performs row-level UPSERTs.
/// Records that succeed are marked as "Synced"; those that fail are marked "Failed".
/// No data is lost if the owner is unreachable — the queue accumulates until
/// the next successful push.
/// </summary>
public class SyncPushService : ISyncPushService
{
    private readonly IUnitOfWork _uow;
    private readonly INetworkConnectivityService _connectivityService;
    private readonly ILogger<SyncPushService> _logger;

    // Compiled regex for extracting the server/host value from simplified connection strings
    // (e.g. "Server=192.168.1.100" or "Data Source=myserver\instance").
    private static readonly Regex ServerValueRegex = new(
        @"(?:Server|Data\s+Source|DataSource)\s*=\s*([^;,]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SyncPushService(
        IUnitOfWork uow,
        INetworkConnectivityService connectivityService,
        ILogger<SyncPushService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SyncAck> PushPendingSyncsAsync(int branchId, int maxRecords = 500)
    {
        var branch = await _uow.Branches.GetByIdAsync(branchId);
        if (branch is null)
            throw new InvalidOperationException($"Branch {branchId} not found");

        var ownerBranch = await _uow.Branches.GetOwnerBranchAsync();
        if (ownerBranch is null)
            throw new InvalidOperationException("Owner branch not configured");

        // Do not push to self if running on owner PC.
        if (branch.IsOwnerBranch)
        {
            _logger.LogInformation("Branch {BranchId} is the owner. Sync push skipped.", branchId);
            return new SyncAck(0, 0, new List<SyncError>(), DateTime.UtcNow);
        }

        // Check LAN reachability before opening the connection.
        var ownerIp = ExtractIpFromConnectionString(ownerBranch.SqlConnectionString);
        var isReachable = await _connectivityService.CanReachOwnerDbAsync(ownerIp);
        if (!isReachable)
        {
            _logger.LogWarning("Owner DB unreachable. Sync deferred.");
            return new SyncAck(0, 0, new List<SyncError>(), DateTime.UtcNow);
        }

        var pendingRecords = (await _uow.SyncQueue.GetPendingSyncsAsync(branchId, maxRecords)).ToList();
        if (pendingRecords.Count == 0)
        {
            _logger.LogInformation("No pending syncs for branch {BranchId}", branchId);
            return new SyncAck(0, 0, new List<SyncError>(), DateTime.UtcNow);
        }

        var errors = new List<SyncError>();
        var syncedCount = 0;

        using var ownerConnection = new SqlConnection(ownerBranch.SqlConnectionString);
        await ownerConnection.OpenAsync();

        foreach (var record in pendingRecords)
        {
            SyncPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<SyncPayload>(record.Payload);
                if (payload is null) throw new InvalidOperationException("Null payload after deserialization");

                await UpsertRecordAsync(ownerConnection, payload, branchId);
                await _uow.SyncQueue.MarkAsSyncedAsync(record.QueueId);
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed for {Table} {Id}",
                    payload?.TableName ?? record.TableName,
                    payload?.RecordId ?? record.RecordId);

                errors.Add(new SyncError(
                    QueueId: record.QueueId,
                    TableName: payload?.TableName ?? record.TableName,
                    RecordId: payload?.RecordId ?? record.RecordId,
                    ErrorMessage: ex.Message));

                await _uow.SyncQueue.MarkAsFailedAsync(record.QueueId, ex.Message);
            }
        }

        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Synced {SyncedCount} records to owner. Errors: {ErrorCount}",
            syncedCount, errors.Count);

        return new SyncAck(syncedCount, errors.Count, errors, DateTime.UtcNow);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static async Task UpsertRecordAsync(
        SqlConnection connection,
        SyncPayload payload,
        int branchId)
    {
        var sql = BuildUpsertSql(payload.TableName, payload.Data.Keys.ToList());

        using var command = new SqlCommand(sql, connection);
        foreach (var kvp in payload.Data)
        {
            var value = kvp.Value is JsonElement je
                ? ExtractJsonElement(je)
                : kvp.Value;
            command.Parameters.AddWithValue($"@{kvp.Key}", value ?? DBNull.Value);
        }
        command.Parameters.AddWithValue("@BranchId", branchId);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Builds a MERGE (UPSERT) statement for the given table and column list.
    /// The "Id" column is used as the match key.
    /// </summary>
    private static string BuildUpsertSql(string tableName, List<string> columns)
    {
        // Guard against SQL injection via table/column names (only allow word chars).
        if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^\w+$"))
            throw new ArgumentException($"Invalid table name: {tableName}");

        var setClauses = columns
            .Where(c => c != "Id")
            .Select(c => $"[{c}] = source.[{c}]")
            .ToList();

        var insertCols = string.Join(", ", columns.Select(c => $"[{c}]"));
        var insertVals = string.Join(", ", columns.Select(c => $"source.[{c}]"));
        var setClause = string.Join(", ", setClauses);

        return $@"
MERGE INTO [{tableName}] AS target
USING (SELECT {string.Join(", ", columns.Select(c => $"@{c} AS [{c}]"))}) AS source
ON target.[Id] = source.[Id]
WHEN MATCHED THEN
    UPDATE SET {setClause}
WHEN NOT MATCHED THEN
    INSERT ({insertCols}) VALUES ({insertVals});";
    }

    private static object? ExtractJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };

    public static string ExtractIpFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "localhost";

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.DataSource?.Split(',')[0].Trim() ?? "localhost";
        }
        catch (ArgumentException)
        {
            // Fall back to regex for simplified / non-standard connection strings
            // (e.g. "Server=192.168.1.100" without additional key-value pairs).
            var match = ServerValueRegex.Match(connectionString);
            return match.Success ? match.Groups[1].Value.Split(',')[0].Trim() : "localhost";
        }
    }
}
