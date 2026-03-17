using GoldSystem.Core.Models;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Thin data-access wrapper dedicated to the Billing Screen.
/// Provides item lookup (by tag / HUID) and customer search without
/// tying BillingViewModel to the full IUnitOfWork interface.
/// </summary>
public class BillingScreenService
{
    private readonly IUnitOfWork _uow;

    public BillingScreenService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    /// <summary>
    /// Looks up an in-stock item by tag number or HUID.
    /// Returns null when the item does not exist or is already sold.
    /// </summary>
    public async Task<ItemDto?> LookupItemAsync(string tagOrHuid, int branchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tagOrHuid))
            return null;

        var byTag = await _uow.Items.GetByTagAsync(tagOrHuid.Trim(), cancellationToken);
        var item = byTag ?? await _uow.Items.GetByHUIDAsync(tagOrHuid.Trim(), cancellationToken);

        if (item is null) return null;
        if (item.Status != "InStock") return null;
        if (item.BranchId != branchId) return null;

        return MapToDto(item);
    }

    /// <summary>
    /// Returns customers whose name or phone starts with the given query.
    /// Limits to 20 results for autocomplete performance.
    /// </summary>
    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string query, int branchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<CustomerDto>();

        var all = await _uow.Customers.GetAllAsync(cancellationToken);
        var q = query.Trim().ToLowerInvariant();

        return all
            .Where(c => c.BranchId == branchId &&
                        (c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                         c.Phone.StartsWith(q, StringComparison.OrdinalIgnoreCase)))
            .Take(20)
            .Select(c => new CustomerDto(c.CustomerId, c.Name, c.Phone));
    }

    /// <summary>Returns the current 24K rate for a branch, or null if unavailable.</summary>
    public async Task<decimal?> GetCurrentRate24KAsync(int branchId,
        CancellationToken cancellationToken = default)
    {
        var rate = await _uow.GoldRates.GetLatestRateAsync(branchId, cancellationToken);
        return rate?.Rate24K;
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static ItemDto MapToDto(Item item) => new(
        ItemId: item.ItemId,
        HUID: item.HUID ?? string.Empty,
        TagNo: item.TagNo,
        Name: item.Name,
        Purity: item.Purity,
        GrossWeight: item.GrossWeight,
        StoneWeight: item.StoneWeight,
        Status: item.Status,
        CreatedAt: item.CreatedAt);
}
