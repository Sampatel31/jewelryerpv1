using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.RateEngine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Accepts manual gold rate entries from operators, validates them,
/// persists them to the database, creates an audit log, and publishes a
/// <see cref="RateChangeEvent"/> so the UI refreshes immediately.
/// </summary>
public class ManualRateEntryService
{
    private readonly IDbContextFactory<GoldDbContext> _dbFactory;
    private readonly RateChangedEventPublisher _publisher;
    private readonly ILogger<ManualRateEntryService> _logger;

    public ManualRateEntryService(
        IDbContextFactory<GoldDbContext> dbFactory,
        RateChangedEventPublisher publisher,
        ILogger<ManualRateEntryService> logger)
    {
        _dbFactory = dbFactory;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Stores a manually entered 24 K rate and derives 22 K and 18 K automatically.
    /// </summary>
    /// <param name="rate24K">Operator-entered 24 K rate (per 10 g).</param>
    /// <param name="branchId">Branch the rate applies to.</param>
    /// <param name="operatorUserId">UserId of the operator entering the rate.</param>
    /// <param name="overrideNote">Optional reason / note for the override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated RateId.</returns>
    public async Task<int> EnterRateAsync(decimal rate24K, int branchId, int operatorUserId,
        string? overrideNote = null, CancellationToken ct = default)
    {
        if (rate24K <= 0)
            throw new ArgumentException("Rate must be greater than zero.", nameof(rate24K));

        var rate22K = Math.Round(rate24K * 22m / 24m, 2);
        var rate18K = Math.Round(rate24K * 18m / 24m, 2);
        var fetchedAt = DateTime.UtcNow;

        var rateResult = new GoldRateResult(rate24K, rate22K, rate18K, "MANUAL_OVERRIDE", fetchedAt);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entity = new GoldRate
        {
            RateDate = DateOnly.FromDateTime(fetchedAt),
            RateTime = TimeOnly.FromDateTime(fetchedAt),
            Rate24K = rateResult.Rate24K,
            Rate22K = rateResult.Rate22K,
            Rate18K = rateResult.Rate18K,
            Source = rateResult.Source,
            IsManualOverride = true,
            OverrideNote = overrideNote,
            BranchId = branchId,
            CreatedBy = operatorUserId,
            CreatedAt = fetchedAt
        };

        db.GoldRates.Add(entity);
        await db.SaveChangesAsync(ct);

        // Audit log created after first save so we have the real RateId.
        var audit = new AuditLog
        {
            UserId = operatorUserId,
            Action = "MANUAL_RATE_ENTRY",
            TableName = "GoldRates",
            RecordId = entity.RateId,
            NewValueJson = JsonSerializer.Serialize(rateResult),
            BranchId = branchId,
            CreatedAt = fetchedAt
        };
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Manual rate entry by user {UserId}: 24K={Rate24K}, branchId={BranchId}",
            operatorUserId, rate24K, branchId);

        _publisher.Publish(new RateChangeEvent
        {
            Rate = rateResult,
            ChangedAt = fetchedAt,
            ChangedBy = "MANUAL_OVERRIDE"
        });

        return entity.RateId;
    }
}
