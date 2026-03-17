namespace GoldSystem.Core.Models;

// ─── Day Book ────────────────────────────────────────────────────────────────

/// <summary>A single row in the daily bill register.</summary>
public record DayBookLine(
    DateOnly Date,
    string BillNo,
    string CustomerName,
    decimal Amount,
    decimal AmountPaid,
    string PaymentMode,
    string Status);

// ─── Sales Register ──────────────────────────────────────────────────────────

/// <summary>An item-level row in the sales register, aggregated by item/category.</summary>
public record SalesRegisterLine(
    string ItemName,
    decimal Quantity,
    decimal GrossWeight,
    decimal NetWeight,
    string Purity,
    decimal Revenue,
    string Category);

// ─── Customer Ledger Report ──────────────────────────────────────────────────

/// <summary>Customer-level outstanding summary for the ledger report.</summary>
public record LedgerReportLine(
    string CustomerName,
    int CustomerId,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal OutstandingAmount,
    decimal OutstandingPercent,
    int AgeInDays);

// ─── Ageing Buckets ──────────────────────────────────────────────────────────

/// <summary>Outstanding amount grouped by age bucket (0-30, 31-60, 61-90, 90+ days).</summary>
public record AgeingBucket(
    string Range,
    int Count,
    decimal Amount,
    decimal Percentage);

// ─── GSTR-1 ──────────────────────────────────────────────────────────────────

/// <summary>One invoice row for GSTR-1 filing.</summary>
public record GSTR1Line(
    string InvoiceNo,
    DateOnly InvoiceDate,
    string? CustomerGSTIN,
    string CustomerName,
    string HSNCode,
    decimal TaxableValue,
    decimal CGST,
    decimal SGST,
    decimal IGST,
    decimal TotalInvoiceValue,
    string SupplyType);

/// <summary>Full GSTR-1 summary for a given month/year period.</summary>
public record GSTR1Summary(
    string Period,
    string GSTIN,
    decimal IntraStateTaxable,
    decimal IntraStateCGST,
    decimal IntraStateSGST,
    decimal InterStateTaxable,
    decimal InterStateIGST,
    decimal ExemptTaxable,
    decimal TotalTaxable,
    decimal TotalTax,
    List<GSTR1Line> Invoices);
