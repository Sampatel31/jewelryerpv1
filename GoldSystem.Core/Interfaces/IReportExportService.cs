using GoldSystem.Core.Models;

namespace GoldSystem.Core.Interfaces;

/// <summary>
/// Exports report data to PDF (QuestPDF), Excel (ClosedXML) or JSON formats.
/// All methods are synchronous byte-array producers suitable for file-save dialogs.
/// </summary>
public interface IReportExportService
{
    // ── Day Book ──────────────────────────────────────────────────────────────

    byte[] ExportDayBookToPdf(IReadOnlyList<DayBookLine> lines, DateOnly date, string branchName);
    byte[] ExportDayBookToExcel(IReadOnlyList<DayBookLine> lines, DateOnly date);

    // ── Sales Register ────────────────────────────────────────────────────────

    byte[] ExportSalesRegisterToPdf(
        IReadOnlyList<SalesRegisterLine> lines, DateOnly from, DateOnly to, string branchName);
    byte[] ExportSalesRegisterToExcel(
        IReadOnlyList<SalesRegisterLine> lines, DateOnly from, DateOnly to);

    // ── Customer Ledger ───────────────────────────────────────────────────────

    byte[] ExportLedgerToPdf(
        IReadOnlyList<LedgerReportLine> lines,
        IReadOnlyList<AgeingBucket> ageing,
        string branchName);
    byte[] ExportLedgerToExcel(
        IReadOnlyList<LedgerReportLine> lines,
        IReadOnlyList<AgeingBucket> ageing);

    // ── GSTR-1 ────────────────────────────────────────────────────────────────

    string ExportGSTR1ToJson(GSTR1Summary summary);
    byte[] ExportGSTR1ToExcel(GSTR1Summary summary);
}
