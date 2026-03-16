using GoldSystem.Data.Entities;
using GoldSystem.Sync.Models;
using GoldSystem.Sync.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="ConflictResolver"/>.
/// </summary>
public class ConflictResolverTests
{
    private readonly ConflictResolver _resolver =
        new(NullLogger<ConflictResolver>.Instance);

    // ── Last-write-wins (generic entities) ─────────────────────────────────────

    [Fact]
    public void ResolveConflict_LocalNewer_ReturnsLocal()
    {
        var local    = new Customer { CustomerId = 1, Name = "Old",     CreatedAt = DateTime.UtcNow };
        var incoming = new Customer { CustomerId = 1, Name = "Updated", CreatedAt = DateTime.UtcNow.AddHours(-1) };

        var result = _resolver.ResolveConflict(local, incoming, "Customer");

        Assert.True(result.HasConflict);
        Assert.Same(local, result.Winner);
        Assert.Contains("Local", result.Resolution);
    }

    [Fact]
    public void ResolveConflict_IncomingNewer_ReturnsIncoming()
    {
        var local    = new Customer { CustomerId = 1, Name = "Old",     CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var incoming = new Customer { CustomerId = 1, Name = "Updated", CreatedAt = DateTime.UtcNow };

        var result = _resolver.ResolveConflict(local, incoming, "Customer");

        Assert.True(result.HasConflict);
        Assert.Same(incoming, result.Winner);
        Assert.Contains("Incoming", result.Resolution);
    }

    // ── GoldRate conflict ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveConflict_GoldRate_OwnerMcxWins()
    {
        var local    = new GoldRate { RateId = 1, Source = "MANUAL", IsManualOverride = true,  CreatedAt = DateTime.UtcNow };
        var incoming = new GoldRate { RateId = 1, Source = "MCX_SCRAPER", IsManualOverride = false, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };

        var result = _resolver.ResolveConflict(local, incoming, "GoldRate");

        Assert.True(result.HasConflict);
        Assert.Same(incoming, result.Winner);
        Assert.Contains("MCX", result.Resolution);
    }

    [Fact]
    public void ResolveConflict_GoldRate_ManualOverrideLocalNewer_LocalWins()
    {
        var local    = new GoldRate { RateId = 1, Source = "MANUAL", IsManualOverride = true, CreatedAt = DateTime.UtcNow };
        var incoming = new GoldRate { RateId = 1, Source = "MANUAL", IsManualOverride = true, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        var result = _resolver.ResolveConflict(local, incoming, "GoldRate");

        Assert.True(result.HasConflict);
        Assert.Same(local, result.Winner);
        Assert.Contains("Local", result.Resolution);
    }

    // ── Item conflict ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveConflict_SoldItemProtected_LocalAlwaysWins()
    {
        var local    = new Item { ItemId = 5, Status = "Sold", SoldBillId = 10, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var incoming = new Item { ItemId = 5, Status = "Available",              CreatedAt = DateTime.UtcNow };

        var result = _resolver.ResolveConflict(local, incoming, "Item");

        Assert.True(result.HasConflict);
        Assert.Same(local, result.Winner);
        Assert.Contains("sold", result.Resolution, StringComparison.OrdinalIgnoreCase);
    }

    // ── Bill / BillItem – no conflict ─────────────────────────────────────────

    [Fact]
    public void ResolveConflict_Bill_ReturnsNoConflict()
    {
        var local    = new Bill { BillId = 1, BillNo = "B1-001" };
        var incoming = new Bill { BillId = 2, BillNo = "B2-001" };

        var result = _resolver.ResolveConflict(local, incoming, "Bill");

        Assert.False(result.HasConflict);
        Assert.Same(incoming, result.Winner);
    }

    [Fact]
    public void ResolveConflict_BillItem_ReturnsNoConflict()
    {
        var local    = new BillItem { BillItemId = 1 };
        var incoming = new BillItem { BillItemId = 1 };

        var result = _resolver.ResolveConflict(local, incoming, "BillItem");

        Assert.False(result.HasConflict);
        Assert.Same(incoming, result.Winner);
    }
}
