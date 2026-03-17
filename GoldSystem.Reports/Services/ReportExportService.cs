using System.Text.Json;
using ClosedXML.Excel;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GoldSystem.Reports.Services;

/// <summary>
/// Exports Phase 12 report data to PDF (QuestPDF), Excel (ClosedXML) and JSON.
/// </summary>
public class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DAY BOOK
    // ═════════════════════════════════════════════════════════════════════════

    public byte[] ExportDayBookToPdf(IReadOnlyList<DayBookLine> lines, DateOnly date, string branchName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ReportHeader(c, "Day Book", branchName, $"Date: {date:dd-MMM-yyyy}"));
                page.Content().PaddingTop(8).Element(c => DayBookTable(c, lines));
                page.Footer().Element(StandardFooter);
            });
        }).GeneratePdf();
    }

    public byte[] ExportDayBookToExcel(IReadOnlyList<DayBookLine> lines, DateOnly date)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Day Book");

        // Header row
        var headers = new[] { "Date", "Bill No", "Customer Name", "Amount (₹)", "Paid (₹)", "Balance (₹)", "Payment Mode", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        int row = 2;
        foreach (var line in lines)
        {
            ws.Cell(row, 1).Value = line.Date.ToString("dd-MMM-yyyy");
            ws.Cell(row, 2).Value = line.BillNo;
            ws.Cell(row, 3).Value = line.CustomerName;
            ws.Cell(row, 4).Value = (double)line.Amount;
            ws.Cell(row, 5).Value = (double)line.AmountPaid;
            ws.Cell(row, 6).Value = (double)(line.Amount - line.AmountPaid);
            ws.Cell(row, 7).Value = line.PaymentMode;
            ws.Cell(row, 8).Value = line.Status;

            // Currency format
            ws.Cell(row, 4).Style.NumberFormat.Format = "₹#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "₹#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "₹#,##0.00";

            // Color unpaid rows
            if (line.AmountPaid < line.Amount)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;

            row++;
        }

        // Totals row
        ws.Cell(row, 3).Value = "TOTAL";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).FormulaA1 = $"=SUM(D2:D{row - 1})";
        ws.Cell(row, 5).FormulaA1 = $"=SUM(E2:E{row - 1})";
        ws.Cell(row, 6).FormulaA1 = $"=SUM(F2:F{row - 1})";
        for (int c = 3; c <= 6; c++)
        {
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SALES REGISTER
    // ═════════════════════════════════════════════════════════════════════════

    public byte[] ExportSalesRegisterToPdf(
        IReadOnlyList<SalesRegisterLine> lines, DateOnly from, DateOnly to, string branchName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ReportHeader(c, "Sales Register", branchName,
                    $"Period: {from:dd-MMM-yyyy} to {to:dd-MMM-yyyy}"));
                page.Content().PaddingTop(8).Element(c => SalesRegisterTable(c, lines));
                page.Footer().Element(StandardFooter);
            });
        }).GeneratePdf();
    }

    public byte[] ExportSalesRegisterToExcel(
        IReadOnlyList<SalesRegisterLine> lines, DateOnly from, DateOnly to)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Register");

        var headers = new[] { "Item Name", "Category", "Purity", "Qty", "Gross Wt (g)", "Net Wt (g)", "Revenue (₹)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var line in lines)
        {
            ws.Cell(row, 1).Value = line.ItemName;
            ws.Cell(row, 2).Value = line.Category;
            ws.Cell(row, 3).Value = line.Purity;
            ws.Cell(row, 4).Value = (double)line.Quantity;
            ws.Cell(row, 5).Value = (double)line.GrossWeight;
            ws.Cell(row, 6).Value = (double)line.NetWeight;
            ws.Cell(row, 7).Value = (double)line.Revenue;
            ws.Cell(row, 7).Style.NumberFormat.Format = "₹#,##0.00";
            row++;
        }

        ws.Cell(row, 6).Value = "TOTAL";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).FormulaA1 = $"=SUM(G2:G{row - 1})";
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CUSTOMER LEDGER
    // ═════════════════════════════════════════════════════════════════════════

    public byte[] ExportLedgerToPdf(
        IReadOnlyList<LedgerReportLine> lines,
        IReadOnlyList<AgeingBucket> ageing,
        string branchName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ReportHeader(c, "Customer Outstanding Ledger", branchName,
                    $"As of {DateTime.Today:dd-MMM-yyyy}"));
                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => LedgerTable(c, lines));
                    col.Item().PaddingTop(16).Element(c => AgeingTable(c, ageing));
                });
                page.Footer().Element(StandardFooter);
            });
        }).GeneratePdf();
    }

    public byte[] ExportLedgerToExcel(
        IReadOnlyList<LedgerReportLine> lines,
        IReadOnlyList<AgeingBucket> ageing)
    {
        using var wb = new XLWorkbook();

        // ── Sheet 1: Outstanding ──────────────────────────────────────────────
        var ws1 = wb.Worksheets.Add("Outstanding");
        var headers = new[] { "Customer", "Customer ID", "Total Billed (₹)", "Total Paid (₹)", "Outstanding (₹)", "Outstanding %", "Age (Days)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws1.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var line in lines)
        {
            ws1.Cell(row, 1).Value = line.CustomerName;
            ws1.Cell(row, 2).Value = line.CustomerId;
            ws1.Cell(row, 3).Value = (double)line.TotalBilled;
            ws1.Cell(row, 4).Value = (double)line.TotalPaid;
            ws1.Cell(row, 5).Value = (double)line.OutstandingAmount;
            ws1.Cell(row, 6).Value = (double)line.OutstandingPercent / 100.0;
            ws1.Cell(row, 7).Value = line.AgeInDays;

            ws1.Cell(row, 3).Style.NumberFormat.Format = "₹#,##0.00";
            ws1.Cell(row, 4).Style.NumberFormat.Format = "₹#,##0.00";
            ws1.Cell(row, 5).Style.NumberFormat.Format = "₹#,##0.00";
            ws1.Cell(row, 6).Style.NumberFormat.Format = "0.0%";

            // Conditional formatting: red for 90+ days
            if (line.AgeInDays >= 90)
                ws1.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;

            row++;
        }

        ws1.Columns().AdjustToContents();

        // ── Sheet 2: Ageing ───────────────────────────────────────────────────
        var ws2 = wb.Worksheets.Add("Ageing");
        var ageHeaders = new[] { "Age Range", "Count", "Amount (₹)", "Percentage" };
        for (int i = 0; i < ageHeaders.Length; i++)
        {
            var cell = ws2.Cell(1, i + 1);
            cell.Value = ageHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int ageRow = 2;
        foreach (var bucket in ageing)
        {
            ws2.Cell(ageRow, 1).Value = bucket.Range;
            ws2.Cell(ageRow, 2).Value = bucket.Count;
            ws2.Cell(ageRow, 3).Value = (double)bucket.Amount;
            ws2.Cell(ageRow, 4).Value = (double)bucket.Percentage / 100.0;
            ws2.Cell(ageRow, 3).Style.NumberFormat.Format = "₹#,##0.00";
            ws2.Cell(ageRow, 4).Style.NumberFormat.Format = "0.0%";
            ageRow++;
        }

        ws2.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GSTR-1
    // ═════════════════════════════════════════════════════════════════════════

    public string ExportGSTR1ToJson(GSTR1Summary summary)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        return JsonSerializer.Serialize(summary, options);
    }

    public byte[] ExportGSTR1ToExcel(GSTR1Summary summary)
    {
        using var wb = new XLWorkbook();

        // ── Sheet 1: Invoice Details ──────────────────────────────────────────
        var ws1 = wb.Worksheets.Add("B2B-B2C Invoices");
        var headers = new[]
        {
            "Invoice No", "Invoice Date", "Customer GSTIN", "Customer Name",
            "HSN Code", "Taxable Value (₹)", "CGST (₹)", "SGST (₹)", "IGST (₹)",
            "Total Invoice Value (₹)", "Supply Type"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws1.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var inv in summary.Invoices)
        {
            ws1.Cell(row, 1).Value = inv.InvoiceNo;
            ws1.Cell(row, 2).Value = inv.InvoiceDate.ToString("dd-MMM-yyyy");
            ws1.Cell(row, 3).Value = inv.CustomerGSTIN ?? "URP";  // Unregistered Person
            ws1.Cell(row, 4).Value = inv.CustomerName;
            ws1.Cell(row, 5).Value = inv.HSNCode;
            ws1.Cell(row, 6).Value = (double)inv.TaxableValue;
            ws1.Cell(row, 7).Value = (double)inv.CGST;
            ws1.Cell(row, 8).Value = (double)inv.SGST;
            ws1.Cell(row, 9).Value = (double)inv.IGST;
            ws1.Cell(row, 10).Value = (double)inv.TotalInvoiceValue;
            ws1.Cell(row, 11).Value = inv.SupplyType;

            for (int c = 6; c <= 10; c++)
                ws1.Cell(row, c).Style.NumberFormat.Format = "₹#,##0.00";

            row++;
        }

        ws1.Columns().AdjustToContents();

        // ── Sheet 2: GST Summary ──────────────────────────────────────────────
        var ws2 = wb.Worksheets.Add("GST Summary");
        ws2.Cell(1, 1).Value = "GSTR-1 Summary";
        ws2.Cell(1, 1).Style.Font.Bold = true;
        ws2.Cell(1, 1).Style.Font.FontSize = 14;

        var summaryRows = new[]
        {
            ("Period",                          summary.Period),
            ("GSTIN",                           summary.GSTIN),
            ("",                                ""),
            ("Intra-State Taxable Value",        summary.IntraStateTaxable.ToString("N2")),
            ("Intra-State CGST",                 summary.IntraStateCGST.ToString("N2")),
            ("Intra-State SGST",                 summary.IntraStateSGST.ToString("N2")),
            ("Inter-State Taxable Value",        summary.InterStateTaxable.ToString("N2")),
            ("Inter-State IGST",                 summary.InterStateIGST.ToString("N2")),
            ("Exempt / Nil-Rated Taxable Value", summary.ExemptTaxable.ToString("N2")),
            ("",                                ""),
            ("Total Taxable Value",              summary.TotalTaxable.ToString("N2")),
            ("Total Tax Liability",              summary.TotalTax.ToString("N2")),
            ("Total Invoices",                   summary.Invoices.Count.ToString()),
        };

        int sr = 3;
        foreach (var (label, value) in summaryRows)
        {
            ws2.Cell(sr, 1).Value = label;
            ws2.Cell(sr, 2).Value = value;
            if (!string.IsNullOrEmpty(label)) ws2.Cell(sr, 1).Style.Font.Bold = true;
            sr++;
        }

        ws2.Column(1).Width = 35;
        ws2.Column(2).Width = 20;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PDF helpers (shared layout primitives)
    // ═════════════════════════════════════════════════════════════════════════

    private static void ReportHeader(IContainer c, string title, string branchName, string subtitle)
    {
        c.Column(col =>
        {
            col.Item().AlignCenter().Text(branchName)
                .Bold().FontSize(16).FontColor(Colors.Orange.Darken3);
            col.Item().AlignCenter().Text(title)
                .Bold().FontSize(13).FontColor(Colors.Grey.Darken3);
            col.Item().AlignCenter().Text(subtitle)
                .FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Orange.Darken2);
        });
    }

    private static void StandardFooter(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem()
                .Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
            row.ConstantItem(60).AlignRight().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static void DayBookTable(IContainer container, IReadOnlyList<DayBookLine> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.5f); // Date
                cols.RelativeColumn(2);    // Bill No
                cols.RelativeColumn(3);    // Customer
                cols.RelativeColumn(2);    // Amount
                cols.RelativeColumn(2);    // Paid
                cols.RelativeColumn(2);    // Balance
                cols.RelativeColumn(2);    // Mode
                cols.RelativeColumn(1.5f); // Status
            });

            table.Header(h =>
            {
                foreach (var hdr in new[] { "Date", "Bill No", "Customer", "Amount ₹", "Paid ₹", "Balance ₹", "Mode", "Status" })
                    h.Cell().Element(HeaderCell).Text(hdr).Bold().FontSize(8).FontColor(Colors.White);
            });

            bool alt = false;
            decimal totalAmt = 0, totalPaid = 0;
            foreach (var line in lines)
            {
                var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                table.Cell().Background(bg).Padding(3).Text(line.Date.ToString("dd-MMM")).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.BillNo).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.CustomerName).FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"₹{line.Amount:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"₹{line.AmountPaid:N0}").FontSize(8);
                var balance = line.Amount - line.AmountPaid;
                table.Cell().Background(bg).Padding(3).AlignRight()
                    .Text($"₹{balance:N0}").FontSize(8)
                    .FontColor(balance > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                table.Cell().Background(bg).Padding(3).Text(line.PaymentMode).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.Status).FontSize(8);

                totalAmt  += line.Amount;
                totalPaid += line.AmountPaid;
                alt = !alt;
            }

            // Totals row
            table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten2)
                .Padding(3).Text("TOTAL").Bold().FontSize(8);
            table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"₹{totalAmt:N0}").Bold().FontSize(8);
            table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"₹{totalPaid:N0}").Bold().FontSize(8);
            table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"₹{totalAmt - totalPaid:N0}").Bold().FontSize(8);
            table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten2).Padding(3).Text(string.Empty);
        });
    }

    private static void SalesRegisterTable(IContainer container, IReadOnlyList<SalesRegisterLine> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);    // Item Name
                cols.RelativeColumn(2);    // Category
                cols.RelativeColumn(1.5f); // Purity
                cols.RelativeColumn(1);    // Qty
                cols.RelativeColumn(1.5f); // Gross Wt
                cols.RelativeColumn(1.5f); // Net Wt
                cols.RelativeColumn(2);    // Revenue
            });

            table.Header(h =>
            {
                foreach (var hdr in new[] { "Item Name", "Category", "Purity", "Qty", "Gross Wt (g)", "Net Wt (g)", "Revenue ₹" })
                    h.Cell().Element(HeaderCell).Text(hdr).Bold().FontSize(8).FontColor(Colors.White);
            });

            bool alt = false;
            decimal totalRevenue = 0;
            foreach (var line in lines)
            {
                var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                table.Cell().Background(bg).Padding(3).Text(line.ItemName).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.Category).FontSize(8);
                table.Cell().Background(bg).Padding(3).Text(line.Purity).FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"{line.Quantity:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"{line.GrossWeight:N3}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"{line.NetWeight:N3}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"₹{line.Revenue:N0}").FontSize(8);
                totalRevenue += line.Revenue;
                alt = !alt;
            }

            table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten2)
                .Padding(3).Text("TOTAL REVENUE").Bold().FontSize(8);
            table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"₹{totalRevenue:N0}").Bold().FontSize(8);
        });
    }

    private static void LedgerTable(IContainer container, IReadOnlyList<LedgerReportLine> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);    // Customer
                cols.RelativeColumn(2);    // Total Billed
                cols.RelativeColumn(2);    // Total Paid
                cols.RelativeColumn(2);    // Outstanding
                cols.RelativeColumn(1.5f); // %
                cols.RelativeColumn(1.5f); // Age
            });

            table.Header(h =>
            {
                foreach (var hdr in new[] { "Customer", "Total Billed ₹", "Total Paid ₹", "Outstanding ₹", "Outstanding %", "Age (Days)" })
                    h.Cell().Element(HeaderCell).Text(hdr).Bold().FontSize(8).FontColor(Colors.White);
            });

            bool alt = false;
            foreach (var line in lines)
            {
                var bg = line.AgeInDays >= 90
                    ? Colors.Red.Lighten4
                    : (alt ? Colors.Grey.Lighten4 : Colors.White);

                table.Cell().Background(bg).Padding(3).Text(line.CustomerName).FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"₹{line.TotalBilled:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight().Text($"₹{line.TotalPaid:N0}").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight()
                    .Text($"₹{line.OutstandingAmount:N0}").Bold().FontSize(8).FontColor(Colors.Red.Darken2);
                table.Cell().Background(bg).Padding(3).AlignRight()
                    .Text($"{line.OutstandingPercent:N1}%").FontSize(8);
                table.Cell().Background(bg).Padding(3).AlignRight()
                    .Text($"{line.AgeInDays}").FontSize(8)
                    .FontColor(line.AgeInDays >= 90 ? Colors.Red.Darken2 : Colors.Black);
                alt = !alt;
            }
        });
    }

    private static void AgeingTable(IContainer container, IReadOnlyList<AgeingBucket> ageing)
    {
        container.Column(col =>
        {
            col.Item().Text("Outstanding Ageing Analysis").Bold().FontSize(10);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1.5f);
                });

                table.Header(h =>
                {
                    foreach (var hdr in new[] { "Age Range", "Count", "Amount ₹", "Percentage" })
                        h.Cell().Element(HeaderCell).Text(hdr).Bold().FontSize(8).FontColor(Colors.White);
                });

                foreach (var bucket in ageing)
                {
                    table.Cell().Padding(3).Text(bucket.Range).FontSize(8);
                    table.Cell().Padding(3).AlignRight().Text($"{bucket.Count}").FontSize(8);
                    table.Cell().Padding(3).AlignRight().Text($"₹{bucket.Amount:N0}").FontSize(8);
                    table.Cell().Padding(3).AlignRight().Text($"{bucket.Percentage:N1}%").FontSize(8);
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer c)
        => c.Background(Colors.Orange.Darken3).Padding(4);
}
