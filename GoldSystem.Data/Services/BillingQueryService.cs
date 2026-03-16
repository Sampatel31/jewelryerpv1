using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Services;

/// <summary>
/// Specialized query service for billing reports, revenue analysis, and GST computation.
/// </summary>
public class BillingQueryService
{
    private readonly GoldDbContext _context;

    public BillingQueryService(GoldDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public record DayBookEntry(
        string BillNo,
        DateOnly BillDate,
        string CustomerName,
        decimal GrandTotal,
        decimal AmountPaid,
        decimal BalanceDue,
        string PaymentMode,
        bool IsLocked);

    public record SalesRegisterEntry(
        string BillNo,
        DateOnly BillDate,
        string CustomerName,
        string ItemName,
        string Purity,
        decimal NetWeight,
        decimal GoldValue,
        decimal MakingAmount,
        decimal LineTotal);

    public record PaymentModeSummaryEntry(
        string Mode,
        int TransactionCount,
        decimal TotalAmount);

    public record GSTSummaryEntry(
        DateOnly BillDate,
        string BillNo,
        string CustomerName,
        string? CustomerGSTIN,
        decimal TaxableAmount,
        decimal CGST,
        decimal SGST,
        decimal IGST,
        decimal TotalGST);

    /// <summary>All bills for a specific date at a branch.</summary>
    public async Task<IEnumerable<DayBookEntry>> GetDayBookAsync(DateOnly date, int branchId, CancellationToken cancellationToken = default)
    {
        var bills = await _context.Bills
            .Include(b => b.Customer)
            .Where(b => b.BranchId == branchId && b.BillDate == date)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return bills.Select(b => new DayBookEntry(
            b.BillNo, b.BillDate, b.Customer.Name,
            b.GrandTotal, b.AmountPaid, b.BalanceDue, b.PaymentMode, b.IsLocked));
    }

    /// <summary>Itemised sales register for a date range.</summary>
    public async Task<IEnumerable<SalesRegisterEntry>> GetSalesRegisterAsync(DateOnly fromDate, DateOnly toDate, int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.BillItems
            .Include(bi => bi.Bill).ThenInclude(b => b.Customer)
            .Where(bi => bi.Bill.BranchId == branchId && bi.Bill.BillDate >= fromDate && bi.Bill.BillDate <= toDate)
            .OrderBy(bi => bi.Bill.BillDate)
            .ThenBy(bi => bi.Bill.BillNo)
            .Select(bi => new SalesRegisterEntry(
                bi.Bill.BillNo,
                bi.Bill.BillDate,
                bi.Bill.Customer.Name,
                bi.ItemName,
                bi.Purity,
                bi.NetWeight,
                bi.GoldValue,
                bi.MakingAmount,
                bi.LineTotal))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Totals grouped by payment mode for a date range.</summary>
    public async Task<IEnumerable<PaymentModeSummaryEntry>> GetPaymentModeSummaryAsync(DateOnly fromDate, DateOnly toDate, int branchId, CancellationToken cancellationToken = default)
    {
        var result = await _context.Payments
            .Include(p => p.Bill)
            .Where(p => p.Bill.BranchId == branchId && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .GroupBy(p => p.Mode)
            .Select(g => new { Mode = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        return result.Select(r => new PaymentModeSummaryEntry(r.Mode, r.Count, r.Total));
    }

    /// <summary>Customer purchase and payment ledger for a date range.</summary>
    public async Task<IEnumerable<DayBookEntry>> GetCustomerLedgerAsync(int customerId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var bills = await _context.Bills
            .Include(b => b.Customer)
            .Where(b => b.CustomerId == customerId && b.BillDate >= fromDate && b.BillDate <= toDate)
            .OrderBy(b => b.BillDate)
            .ToListAsync(cancellationToken);

        return bills.Select(b => new DayBookEntry(
            b.BillNo, b.BillDate, b.Customer.Name,
            b.GrandTotal, b.AmountPaid, b.BalanceDue, b.PaymentMode, b.IsLocked));
    }

    /// <summary>GST summary (CGST/SGST/IGST) for a date range.</summary>
    public async Task<IEnumerable<GSTSummaryEntry>> GetGSTSummaryAsync(DateOnly fromDate, DateOnly toDate, int branchId, CancellationToken cancellationToken = default)
    {
        var bills = await _context.Bills
            .Include(b => b.Customer)
            .Where(b => b.BranchId == branchId && b.BillDate >= fromDate && b.BillDate <= toDate)
            .OrderBy(b => b.BillDate)
            .ToListAsync(cancellationToken);

        return bills.Select(b => new GSTSummaryEntry(
            b.BillDate,
            b.BillNo,
            b.Customer.Name,
            b.Customer.GSTIN,
            b.TaxableAmount,
            b.CGST,
            b.SGST,
            b.IGST,
            b.CGST + b.SGST + b.IGST));
    }
}
