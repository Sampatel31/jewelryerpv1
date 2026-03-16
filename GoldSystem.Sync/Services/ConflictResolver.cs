using GoldSystem.Data.Entities;
using GoldSystem.Sync.Models;
using Microsoft.Extensions.Logging;

namespace GoldSystem.Sync.Services;

/// <summary>
/// Resolves data conflicts when the same record is modified on multiple branches.
/// </summary>
public interface IConflictResolver
{
    ConflictResolutionResult ResolveConflict(object localRecord, object incomingRecord, string tableName);
}

/// <summary>
/// Default implementation of <see cref="IConflictResolver"/>.
/// Strategy summary:
/// <list type="bullet">
///   <item><term>GoldRate</term><description>Owner MCX rate wins; otherwise last-write-wins by CreatedAt.</description></item>
///   <item><term>Bill</term><description>No conflict — branch-prefixed BillNo guarantees uniqueness.</description></item>
///   <item><term>Item</term><description>First-writer wins; a sold item is never overwritten.</description></item>
///   <item><term>BillItem</term><description>No conflict — bill items are immutable once created.</description></item>
///   <item><term>All others</term><description>Last-write-wins based on CreatedAt timestamp.</description></item>
/// </list>
/// </summary>
public class ConflictResolver : IConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(ILogger<ConflictResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ConflictResolutionResult ResolveConflict(
        object localRecord,
        object incomingRecord,
        string tableName)
    {
        ArgumentNullException.ThrowIfNull(localRecord);
        ArgumentNullException.ThrowIfNull(incomingRecord);

        try
        {
            return tableName switch
            {
                "GoldRate" => ResolveRateConflict(localRecord as GoldRate, incomingRecord as GoldRate),
                "Bill" => new ConflictResolutionResult(false, incomingRecord, "No conflict - unique bill numbers"),
                "Item" => ResolveItemConflict(localRecord as Item, incomingRecord as Item),
                "BillItem" => new ConflictResolutionResult(false, incomingRecord, "No conflict - bill items immutable"),
                _ => LastWriteWinsResolution(localRecord, incomingRecord)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conflict for {Table}", tableName);
            return new ConflictResolutionResult(true, incomingRecord, $"Error: {ex.Message}");
        }
    }

    private static ConflictResolutionResult ResolveRateConflict(GoldRate? local, GoldRate? incoming)
    {
        if (local is null)
            return new ConflictResolutionResult(false, incoming, "No local record");
        if (incoming is null)
            return new ConflictResolutionResult(false, local, "No incoming record");

        // Owner PC rate fetched from MCX is authoritative.
        if (incoming.Source == "MCX_SCRAPER" && !incoming.IsManualOverride)
            return new ConflictResolutionResult(true, incoming, "Owner MCX rate wins");

        // Otherwise last-write-wins by CreatedAt.
        return local.CreatedAt > incoming.CreatedAt
            ? new ConflictResolutionResult(true, local, "Local rate is newer")
            : new ConflictResolutionResult(true, incoming, "Incoming rate is newer");
    }

    private static ConflictResolutionResult ResolveItemConflict(Item? local, Item? incoming)
    {
        if (local is null)
            return new ConflictResolutionResult(false, incoming, "No local record");
        if (incoming is null)
            return new ConflictResolutionResult(false, local, "No incoming record");

        // Sold item is protected — no branch can overwrite a completed sale.
        if (local.Status == "Sold" && local.SoldBillId.HasValue)
            return new ConflictResolutionResult(true, local, "Item already sold - local wins");

        // Last-write-wins for other state transitions.
        return local.CreatedAt > incoming.CreatedAt
            ? new ConflictResolutionResult(true, local, "Local item is newer")
            : new ConflictResolutionResult(true, incoming, "Incoming item is newer");
    }

    private static ConflictResolutionResult LastWriteWinsResolution(object local, object incoming)
    {
        var createdAtProperty = local.GetType().GetProperty("CreatedAt");

        if (createdAtProperty is null)
            return new ConflictResolutionResult(false, incoming, "No timestamp found");

        var localTime = (DateTime)createdAtProperty.GetValue(local)!;
        var incomingTime = (DateTime)createdAtProperty.GetValue(incoming)!;

        if (incomingTime > localTime)
            return new ConflictResolutionResult(true, incoming, "Incoming record is newer");

        return new ConflictResolutionResult(true, local, "Local record is newer");
    }
}
