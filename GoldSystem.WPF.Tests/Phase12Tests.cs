using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.Reports.Services;
using Moq;

namespace GoldSystem.WPF.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 12 – Report Model Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class Phase12ModelTests
{
    // ── DayBookLine ───────────────────────────────────────────────────────────

    [Fact]
    public void DayBookLine_CreatesCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var line = new DayBookLine(today, "MUM-0001", "John Doe", 50_000m, 50_000m, "Cash", "Paid");

        Assert.Equal("MUM-0001", line.BillNo);
        Assert.Equal("John Doe", line.CustomerName);
        Assert.Equal(50_000m, line.Amount);
        Assert.Equal("Paid", line.Status);
    }

    [Fact]
    public void DayBookLine_BalanceDue_IsZeroWhenFullyPaid()
    {
        var line = new DayBookLine(DateOnly.FromDateTime(DateTime.Today), "MUM-0001", "Alice", 30_000m, 30_000m, "UPI", "Paid");
        Assert.Equal(0m, line.Amount - line.AmountPaid);
    }

    [Fact]
    public void DayBookLine_BalanceDue_IsNonZeroWhenPartiallyPaid()
    {
        var line = new DayBookLine(DateOnly.FromDateTime(DateTime.Today), "MUM-0002", "Bob", 80_000m, 50_000m, "Cash", "Credit");
        Assert.Equal(30_000m, line.Amount - line.AmountPaid);
    }

    // ── SalesRegisterLine ─────────────────────────────────────────────────────

    [Fact]
    public void SalesRegisterLine_CreatesCorrectly()
    {
        var line = new SalesRegisterLine("Gold Chain", 1m, 12.5m, 11.8m, "22K", 72_000m, "Chains");
        Assert.Equal("Gold Chain", line.ItemName);
        Assert.Equal("22K", line.Purity);
        Assert.Equal(72_000m, line.Revenue);
        Assert.Equal("Chains", line.Category);
    }

    [Fact]
    public void SalesRegisterLine_NetWeightLessThanGross()
    {
        var line = new SalesRegisterLine("Ring", 1m, 5.0m, 4.5m, "18K", 22_000m, "Rings");
        Assert.True(line.GrossWeight > line.NetWeight);
    }

    // ── LedgerReportLine ──────────────────────────────────────────────────────

    [Fact]
    public void LedgerReportLine_OutstandingPercent_IsCorrect()
    {
        var line = new LedgerReportLine("Raj Kumar", 42, 100_000m, 75_000m, 25_000m, 25.0m, 25);
        Assert.Equal(25.0m, line.OutstandingPercent);
        Assert.Equal(25, line.AgeInDays);
    }

    [Fact]
    public void LedgerReportLine_FullyOutstanding_Percent100()
    {
        var line = new LedgerReportLine("Priya", 7, 50_000m, 0m, 50_000m, 100.0m, 90);
        Assert.Equal(100.0m, line.OutstandingPercent);
        Assert.Equal(90, line.AgeInDays);
    }

    // ── AgeingBucket ──────────────────────────────────────────────────────────

    [Fact]
    public void AgeingBucket_CreatesCorrectly()
    {
        var bucket = new AgeingBucket("0-30 days", 5, 1_50_000m, 40.0m);
        Assert.Equal("0-30 days", bucket.Range);
        Assert.Equal(5, bucket.Count);
        Assert.Equal(40.0m, bucket.Percentage);
    }

    // ── GSTR1Line ─────────────────────────────────────────────────────────────

    [Fact]
    public void GSTR1Line_IntraState_HasCGSTAndSGST()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var inv = new GSTR1Line("MUM-001", today, null, "Walk-in", "7113",
            50_000m, 750m, 750m, 0m, 51_500m, "INTRA-STATE");

        Assert.Equal("INTRA-STATE", inv.SupplyType);
        Assert.Equal(750m, inv.CGST);
        Assert.Equal(750m, inv.SGST);
        Assert.Equal(0m, inv.IGST);
        Assert.Equal("7113", inv.HSNCode);
    }

    [Fact]
    public void GSTR1Line_InterState_HasIGSTOnly()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var inv = new GSTR1Line("MUM-002", today, "27AAPFU0939F1ZV", "Registered Co",
            "7113", 1_00_000m, 0m, 0m, 3_000m, 1_03_000m, "INTER-STATE");

        Assert.Equal("INTER-STATE", inv.SupplyType);
        Assert.Equal(0m, inv.CGST);
        Assert.Equal(0m, inv.SGST);
        Assert.Equal(3_000m, inv.IGST);
    }

    // ── GSTR1Summary ─────────────────────────────────────────────────────────

    [Fact]
    public void GSTR1Summary_TotalTax_EqualsIntraPlusInter()
    {
        var invoices = new List<GSTR1Line>
        {
            new("MUM-001", DateOnly.FromDateTime(DateTime.Today), null, "Customer A", "7113",
                50_000m, 750m, 750m, 0m, 51_500m, "INTRA-STATE"),
            new("MUM-002", DateOnly.FromDateTime(DateTime.Today), null, "Customer B", "7113",
                30_000m, 0m, 0m, 900m, 30_900m, "INTER-STATE"),
        };

        var summary = new GSTR1Summary(
            "2025-03", "27AAPFU0939F1ZV",
            50_000m, 750m, 750m,
            30_000m, 900m,
            0m,
            80_000m, 2_400m,
            invoices);

        Assert.Equal(2_400m, summary.TotalTax);
        Assert.Equal(80_000m, summary.TotalTaxable);
        Assert.Equal(2, summary.Invoices.Count);
        Assert.Equal("2025-03", summary.Period);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 12 – ReportExportService Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class ReportExportServiceTests
{
    private static readonly ReportExportService _svc = new();

    private static DayBookLine MakeDayBookLine(decimal amount = 50_000m, decimal paid = 50_000m)
        => new(DateOnly.FromDateTime(DateTime.Today), "MUM-0001", "Test Customer", amount, paid, "Cash", "Paid");

    private static SalesRegisterLine MakeSalesLine()
        => new("Gold Ring", 1m, 4.5m, 4.0m, "22K", 25_000m, "Rings");

    private static LedgerReportLine MakeLedgerLine()
        => new("Alice", 1, 1_00_000m, 80_000m, 20_000m, 20.0m, 15);

    private static AgeingBucket MakeBucket()
        => new("0-30 days", 1, 20_000m, 100m);

    // ── Day Book PDF ──────────────────────────────────────────────────────────

    [Fact]
    public void ExportDayBookToPdf_ReturnsNonEmptyBytes()
    {
        var lines = new List<DayBookLine> { MakeDayBookLine() };
        var bytes = _svc.ExportDayBookToPdf(lines, DateOnly.FromDateTime(DateTime.Today), "Test Branch");
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ExportDayBookToPdf_EmptyList_ReturnsPdf()
    {
        var bytes = _svc.ExportDayBookToPdf([], DateOnly.FromDateTime(DateTime.Today), "Branch");
        Assert.NotEmpty(bytes);
    }

    // ── Day Book Excel ────────────────────────────────────────────────────────

    [Fact]
    public void ExportDayBookToExcel_ReturnsNonEmptyBytes()
    {
        var lines = new List<DayBookLine> { MakeDayBookLine(), MakeDayBookLine(70_000m, 50_000m) };
        var bytes = _svc.ExportDayBookToExcel(lines, DateOnly.FromDateTime(DateTime.Today));
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ExportDayBookToExcel_HasValidXlsxMagicBytes()
    {
        var lines = new List<DayBookLine> { MakeDayBookLine() };
        var bytes = _svc.ExportDayBookToExcel(lines, DateOnly.FromDateTime(DateTime.Today));
        // XLSX = ZIP format → magic bytes PK (0x50 0x4B)
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    // ── Sales Register PDF ────────────────────────────────────────────────────

    [Fact]
    public void ExportSalesRegisterToPdf_ReturnsNonEmptyBytes()
    {
        var lines = new List<SalesRegisterLine> { MakeSalesLine() };
        var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var to   = DateOnly.FromDateTime(DateTime.Today);
        var bytes = _svc.ExportSalesRegisterToPdf(lines, from, to, "Test Branch");
        Assert.NotEmpty(bytes);
    }

    // ── Sales Register Excel ──────────────────────────────────────────────────

    [Fact]
    public void ExportSalesRegisterToExcel_ReturnsNonEmptyBytes()
    {
        var lines = new List<SalesRegisterLine> { MakeSalesLine() };
        var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var to   = DateOnly.FromDateTime(DateTime.Today);
        var bytes = _svc.ExportSalesRegisterToExcel(lines, from, to);
        Assert.NotEmpty(bytes);
    }

    // ── Customer Ledger PDF ───────────────────────────────────────────────────

    [Fact]
    public void ExportLedgerToPdf_ReturnsNonEmptyBytes()
    {
        var lines  = new List<LedgerReportLine> { MakeLedgerLine() };
        var ageing = new List<AgeingBucket>     { MakeBucket() };
        var bytes  = _svc.ExportLedgerToPdf(lines, ageing, "Test Branch");
        Assert.NotEmpty(bytes);
    }

    // ── Customer Ledger Excel ─────────────────────────────────────────────────

    [Fact]
    public void ExportLedgerToExcel_ReturnsNonEmptyBytes()
    {
        var lines  = new List<LedgerReportLine> { MakeLedgerLine() };
        var ageing = new List<AgeingBucket>     { MakeBucket() };
        var bytes  = _svc.ExportLedgerToExcel(lines, ageing);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ExportLedgerToExcel_HasValidXlsxMagicBytes()
    {
        var lines  = new List<LedgerReportLine> { MakeLedgerLine() };
        var ageing = new List<AgeingBucket>     { MakeBucket() };
        var bytes  = _svc.ExportLedgerToExcel(lines, ageing);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    // ── GSTR-1 JSON ───────────────────────────────────────────────────────────

    [Fact]
    public void ExportGSTR1ToJson_ReturnsValidJson()
    {
        var summary = BuildGSTR1Summary();
        var json = _svc.ExportGSTR1ToJson(summary);
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"period\"", json);
        Assert.Contains("\"totalTax\"", json);
    }

    [Fact]
    public void ExportGSTR1ToJson_ContainsPeriodAndGSTIN()
    {
        var summary = BuildGSTR1Summary();
        var json = _svc.ExportGSTR1ToJson(summary);
        Assert.Contains("2025-03", json);
        Assert.Contains("27AAPFU0939F1ZV", json);
    }

    // ── GSTR-1 Excel ──────────────────────────────────────────────────────────

    [Fact]
    public void ExportGSTR1ToExcel_ReturnsNonEmptyBytes()
    {
        var summary = BuildGSTR1Summary();
        var bytes = _svc.ExportGSTR1ToExcel(summary);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ExportGSTR1ToExcel_HasValidXlsxMagicBytes()
    {
        var summary = BuildGSTR1Summary();
        var bytes = _svc.ExportGSTR1ToExcel(summary);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GSTR1Summary BuildGSTR1Summary()
    {
        var invoices = new List<GSTR1Line>
        {
            new("MUM-001", DateOnly.FromDateTime(DateTime.Today), null, "Customer A", "7113",
                50_000m, 750m, 750m, 0m, 51_500m, "INTRA-STATE"),
        };
        return new GSTR1Summary(
            "2025-03", "27AAPFU0939F1ZV",
            50_000m, 750m, 750m,
            0m, 0m, 0m,
            50_000m, 1_500m,
            invoices);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 12 – ReportsViewModel Tests
// ═══════════════════════════════════════════════════════════════════════════════

public class ReportsViewModelTests
{
    private static GoldSystem.WPF.Services.NavigationService CreateNav()
    {
        var sp = new Mock<IServiceProvider>();
        return new GoldSystem.WPF.Services.NavigationService(sp.Object);
    }

    private static (GoldSystem.WPF.ViewModels.ReportsViewModel vm,
                    Mock<IReportGenerationService> gen,
                    Mock<IReportExportService> exp) Create()
    {
        var state = new GoldSystem.WPF.Services.AppState();
        var gen   = new Mock<IReportGenerationService>();
        var exp   = new Mock<IReportExportService>();
        var vm    = new GoldSystem.WPF.ViewModels.ReportsViewModel(CreateNav(), state, gen.Object, exp.Object);
        return (vm, gen, exp);
    }

    [Fact]
    public void ReportsViewModel_InitialState_IsEmpty()
    {
        var (vm, _, _) = Create();
        Assert.Empty(vm.DayBookLines);
        Assert.Empty(vm.SalesLines);
        Assert.Empty(vm.LedgerLines);
        Assert.Empty(vm.Gstr1Lines);
    }

    [Fact]
    public async Task GenerateDayBook_PopulatesLines()
    {
        var (vm, gen, _) = Create();
        var today = DateOnly.FromDateTime(DateTime.Today);
        gen.Setup(g => g.GenerateDayBookAsync(It.IsAny<DateOnly>(), It.IsAny<int>(), default))
           .ReturnsAsync(new List<DayBookLine>
           {
               new(today, "MUM-0001", "Alice", 50_000m, 50_000m, "Cash", "Paid"),
               new(today, "MUM-0002", "Bob",   30_000m, 20_000m, "Card", "Credit"),
           });

        await vm.GenerateDayBookCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.DayBookLines.Count);
        Assert.Equal(2, vm.DayBookBillCount);
        Assert.Equal(80_000m, vm.DayBookTotalAmount);
        Assert.Equal(70_000m, vm.DayBookTotalPaid);
        Assert.Equal(10_000m, vm.DayBookTotalOutstanding);
    }

    [Fact]
    public async Task GenerateSalesRegister_PopulatesLines()
    {
        var (vm, gen, _) = Create();
        gen.Setup(g => g.GenerateSalesRegisterAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), default))
           .ReturnsAsync(new List<SalesRegisterLine>
           {
               new("Gold Chain", 1m, 12.5m, 11.8m, "22K", 72_000m, "Chains"),
               new("Gold Ring",  1m,  4.5m,  4.0m, "18K", 25_000m, "Rings"),
           });

        await vm.GenerateSalesRegisterCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.SalesLines.Count);
        Assert.Equal(97_000m, vm.SalesTotalRevenue);
    }

    [Fact]
    public async Task GenerateLedger_PopulatesLinesAndAgeing()
    {
        var (vm, gen, _) = Create();
        gen.Setup(g => g.GenerateLedgerReportAsync(It.IsAny<int>(), default))
           .ReturnsAsync(new List<LedgerReportLine>
           {
               new("Alice", 1, 1_00_000m, 80_000m, 20_000m, 20.0m, 10),
           });
        gen.Setup(g => g.GetLedgerAgeingAsync(It.IsAny<int>(), default))
           .ReturnsAsync(new List<AgeingBucket>
           {
               new("0-30 days", 1, 20_000m, 100.0m),
           });

        await vm.GenerateLedgerCommand.ExecuteAsync(null);

        Assert.Single(vm.LedgerLines);
        Assert.Single(vm.AgeingBuckets);
        Assert.Equal(20_000m, vm.LedgerTotalOutstanding);
        Assert.Equal(1, vm.LedgerCustomerCount);
    }

    [Fact]
    public async Task GenerateGSTR1_PopulatesSummaryAndLines()
    {
        var (vm, gen, _) = Create();
        var summary = new GSTR1Summary(
            "2025-03", "27AAPFU0939F1ZV",
            50_000m, 750m, 750m, 0m, 0m, 0m, 50_000m, 1_500m,
            new List<GSTR1Line>
            {
                new("MUM-001", DateOnly.FromDateTime(DateTime.Today), null, "Customer A",
                    "7113", 50_000m, 750m, 750m, 0m, 51_500m, "INTRA-STATE"),
            });

        gen.Setup(g => g.GenerateGSTR1Async(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), default))
           .ReturnsAsync(summary);

        await vm.GenerateGSTR1Command.ExecuteAsync(null);

        Assert.Equal(1, vm.Gstr1InvoiceCount);
        Assert.Equal(50_000m, vm.Gstr1TotalTaxable);
        Assert.Equal(1_500m, vm.Gstr1TotalTax);
        Assert.NotNull(vm.Gstr1Summary);
    }

    [Fact]
    public async Task GenerateDayBook_OnError_SetsHasError()
    {
        var (vm, gen, _) = Create();
        gen.Setup(g => g.GenerateDayBookAsync(It.IsAny<DateOnly>(), It.IsAny<int>(), default))
           .ThrowsAsync(new InvalidOperationException("DB not available"));

        await vm.GenerateDayBookCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Contains("DB not available", vm.StatusMessage);
    }
}
