using GoldSystem.Core.Models;

namespace GoldSystem.Core.Interfaces;

/// <summary>
/// Generates report data by querying the database and projecting to report models.
/// </summary>
public interface IReportGenerationService
{
    /// <summary>Returns all bills for the given date and branch.</summary>
    Task<IReadOnlyList<DayBookLine>> GenerateDayBookAsync(
        DateOnly date, int branchId, CancellationToken ct = default);

    /// <summary>Returns item-level sales lines for the given date range.</summary>
    Task<IReadOnlyList<SalesRegisterLine>> GenerateSalesRegisterAsync(
        DateOnly from, DateOnly to, int branchId, CancellationToken ct = default);

    /// <summary>Returns customer-level outstanding summary for the branch.</summary>
    Task<IReadOnlyList<LedgerReportLine>> GenerateLedgerReportAsync(
        int branchId, CancellationToken ct = default);

    /// <summary>Generates a GSTR-1 summary for the given month/year.</summary>
    Task<GSTR1Summary> GenerateGSTR1Async(
        int month, int year, int branchId, CancellationToken ct = default);

    /// <summary>Returns the outstanding amount broken into ageing buckets.</summary>
    Task<IReadOnlyList<AgeingBucket>> GetLedgerAgeingAsync(
        int branchId, CancellationToken ct = default);
}
