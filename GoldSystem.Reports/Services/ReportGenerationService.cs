using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Reports.Services;

/// <summary>
/// Queries the database and projects data into Phase 12 report models.
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private readonly GoldDbContext _db;

    // Gold jewellery HSN code (articles of jewellery of precious metal)
    private const string GoldHSN = "7113";

    public ReportGenerationService(GoldDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // ─── Day Book ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DayBookLine>> GenerateDayBookAsync(
        DateOnly date, int branchId, CancellationToken ct = default)
    {
        var bills = await _db.Bills
            .Include(b => b.Customer)
            .Where(b => b.BranchId == branchId && b.BillDate == date)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);

        return bills.Select(b => new DayBookLine(
            b.BillDate,
            b.BillNo,
            b.Customer.Name,
            b.GrandTotal,
            b.AmountPaid,
            b.PaymentMode,
            b.Status)).ToList();
    }

    // ─── Sales Register ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesRegisterLine>> GenerateSalesRegisterAsync(
        DateOnly from, DateOnly to, int branchId, CancellationToken ct = default)
    {
        var items = await _db.BillItems
            .Include(bi => bi.Bill)
            .Include(bi => bi.Item).ThenInclude(i => i.Category)
            .Where(bi => bi.Bill.BranchId == branchId
                      && bi.Bill.BillDate >= from
                      && bi.Bill.BillDate <= to)
            .OrderBy(bi => bi.Bill.BillDate)
            .ThenBy(bi => bi.Bill.BillNo)
            .ToListAsync(ct);

        return items.Select(bi => new SalesRegisterLine(
            bi.ItemName,
            1m,                                             // one item per bill-item row
            bi.GrossWeight,
            bi.NetWeight,
            bi.Purity,
            bi.LineTotal,
            bi.Item.Category?.Name ?? "Other")).ToList();
    }

    // ─── Customer Ledger ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LedgerReportLine>> GenerateLedgerReportAsync(
        int branchId, CancellationToken ct = default)
    {
        // Fetch customers with outstanding amounts at the branch
        var customers = await _db.Customers
            .Include(c => c.Bills)
            .Where(c => c.BranchId == branchId)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = new List<LedgerReportLine>();
        foreach (var cust in customers)
        {
            var bills = cust.Bills.ToList();
            if (bills.Count == 0) continue;

            decimal totalBilled = bills.Sum(b => b.GrandTotal);
            decimal totalPaid   = bills.Sum(b => b.AmountPaid);
            decimal outstanding = bills.Sum(b => b.BalanceDue);

            if (outstanding <= 0m) continue;   // skip fully paid customers

            // Age = days since earliest unpaid bill
            var oldestUnpaidDate = bills
                .Where(b => b.BalanceDue > 0)
                .Select(b => b.BillDate)
                .DefaultIfEmpty(today)
                .Min();

            int ageInDays = today.DayNumber - oldestUnpaidDate.DayNumber;
            decimal outstandingPct = totalBilled > 0
                ? Math.Round(outstanding / totalBilled * 100m, 1)
                : 0m;

            result.Add(new LedgerReportLine(
                cust.Name,
                cust.CustomerId,
                totalBilled,
                totalPaid,
                outstanding,
                outstandingPct,
                ageInDays));
        }

        return result.OrderByDescending(r => r.OutstandingAmount).ToList();
    }

    // ─── GSTR-1 ───────────────────────────────────────────────────────────────

    public async Task<GSTR1Summary> GenerateGSTR1Async(
        int month, int year, int branchId, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to   = from.AddMonths(1).AddDays(-1);

        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId, ct);
        string gstin = branch?.GSTIN ?? string.Empty;

        var bills = await _db.Bills
            .Include(b => b.Customer)
            .Where(b => b.BranchId == branchId
                     && b.BillDate >= from
                     && b.BillDate <= to)
            .OrderBy(b => b.BillDate)
            .ToListAsync(ct);

        var lines = bills.Select(b =>
        {
            string supplyType = b.IGST > 0 ? "INTER-STATE" : "INTRA-STATE";
            return new GSTR1Line(
                b.BillNo,
                b.BillDate,
                b.Customer.GSTIN,
                b.Customer.Name,
                GoldHSN,
                b.TaxableAmount,
                b.CGST,
                b.SGST,
                b.IGST,
                b.GrandTotal,
                supplyType);
        }).ToList();

        decimal intraStateTaxable = lines.Where(l => l.SupplyType == "INTRA-STATE").Sum(l => l.TaxableValue);
        decimal intraStateCGST    = lines.Where(l => l.SupplyType == "INTRA-STATE").Sum(l => l.CGST);
        decimal intraStateSGST    = lines.Where(l => l.SupplyType == "INTRA-STATE").Sum(l => l.SGST);
        decimal interStateTaxable = lines.Where(l => l.SupplyType == "INTER-STATE").Sum(l => l.TaxableValue);
        decimal interStateIGST    = lines.Where(l => l.SupplyType == "INTER-STATE").Sum(l => l.IGST);
        decimal exemptTaxable     = 0m;  // no exempt supplies in current model

        decimal totalTaxable = intraStateTaxable + interStateTaxable + exemptTaxable;
        decimal totalTax     = intraStateCGST + intraStateSGST + interStateIGST;

        string period = $"{year}-{month:D2}";

        return new GSTR1Summary(
            period,
            gstin,
            intraStateTaxable,
            intraStateCGST,
            intraStateSGST,
            interStateTaxable,
            interStateIGST,
            exemptTaxable,
            totalTaxable,
            totalTax,
            lines);
    }

    // ─── Ledger Ageing ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AgeingBucket>> GetLedgerAgeingAsync(
        int branchId, CancellationToken ct = default)
    {
        var ledgerLines = await GenerateLedgerReportAsync(branchId, ct);

        var buckets = new[]
        {
            ("0-30 days",  0,  30),
            ("31-60 days", 31, 60),
            ("61-90 days", 61, 90),
            ("90+ days",   91, int.MaxValue),
        };

        decimal totalOutstanding = ledgerLines.Sum(l => l.OutstandingAmount);

        return buckets.Select(b =>
        {
            var matches = ledgerLines.Where(l => l.AgeInDays >= b.Item2 && l.AgeInDays <= b.Item3).ToList();
            decimal amount = matches.Sum(l => l.OutstandingAmount);
            decimal pct = totalOutstanding > 0 ? Math.Round(amount / totalOutstanding * 100m, 1) : 0m;
            return new AgeingBucket(b.Item1, matches.Count, amount, pct);
        }).ToList();
    }
}
