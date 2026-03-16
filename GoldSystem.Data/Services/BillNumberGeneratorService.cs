using GoldSystem.Core.Services;

namespace GoldSystem.Data.Services;

/// <summary>
/// Generates unique bill numbers in the format: {BranchCode}-{FY}-{5-digit-serial}
/// Example: MUM-2526-00431
/// FY follows Indian fiscal year (April–March). E.g. April 2026 → FY 2627,
/// March 2026 → FY 2526.
/// Serial resets daily: counts today's existing bills + 1.
/// </summary>
public class BillNumberGeneratorService : IBillNumberGenerator
{
    private readonly IUnitOfWork _uow;

    public BillNumberGeneratorService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<string> GenerateBillNoAsync(int branchId)
    {
        var branch = await _uow.Branches.GetByIdAsync(branchId)
            ?? throw new InvalidOperationException($"Branch {branchId} not found");

        var now = DateTime.Now;

        // Indian fiscal year starts April 1st
        int fyStartYear = now.Month < 4 ? now.Year - 1 : now.Year;
        var fy = (fyStartYear % 100).ToString().PadLeft(2, '0')
               + ((fyStartYear + 1) % 100).ToString().PadLeft(2, '0');

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todaysBills = await _uow.Bills.GetBillsByDateRangeAsync(today, today, branchId);
        var serial = todaysBills.Count() + 1;

        return $"{branch.Code}-{fy}-{serial:D5}";
    }
}
