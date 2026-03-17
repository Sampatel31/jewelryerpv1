using GoldSystem.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GoldSystem.Reports.Services;

/// <summary>
/// Generates professional PDF bill documents using QuestPDF.
/// Layout: shop header → customer section → items table →
///         GST breakdown → payment summary → footer.
/// </summary>
public class BillPdfService : IBillPdfService
{
    static BillPdfService()
    {
        // QuestPDF community licence – free for open-source / personal use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateBillPdf(BillDto bill, string shopName, string shopAddress, string shopPhone)
    {
        var document = BuildDocument(bill, shopName, shopAddress, shopPhone);
        return document.GeneratePdf();
    }

    public void SaveBillPdf(BillDto bill, string filePath, string shopName, string shopAddress, string shopPhone)
    {
        var document = BuildDocument(bill, shopName, shopAddress, shopPhone);
        document.GeneratePdf(filePath);
    }

    // ─── Document builder ────────────────────────────────────────────────────

    private static IDocument BuildDocument(BillDto bill, string shopName, string shopAddress, string shopPhone)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, bill, shopName, shopAddress, shopPhone));
                page.Content().Element(c => ComposeContent(c, bill));
                page.Footer().Element(ComposeFooter);
            });
        });

    // ─── Header: shop info + bill meta ──────────────────────────────────────

    private static void ComposeHeader(IContainer container, BillDto bill,
        string shopName, string shopAddress, string shopPhone)
    {
        container.Column(col =>
        {
            // Shop name
            col.Item().AlignCenter().Text(shopName)
                .Bold().FontSize(18).FontColor(Colors.Orange.Darken3);

            col.Item().AlignCenter().Text(shopAddress)
                .FontSize(8).FontColor(Colors.Grey.Darken2);

            col.Item().AlignCenter().Text($"Phone: {shopPhone}")
                .FontSize(8).FontColor(Colors.Grey.Darken2);

            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Orange.Darken2);

            // Bill meta row
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Bill No: {bill.BillNo}").Bold();
                    c.Item().Text($"Date: {bill.BillDate:dd-MMM-yyyy}");
                    c.Item().Text($"Status: {bill.Status}");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Customer: {bill.Customer.Name}").Bold();
                    c.Item().Text($"Phone: {bill.Customer.Phone}");
                    c.Item().Text($"Payment: {bill.PaymentMode}");
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Rate 22K: ₹{bill.Items.FirstOrDefault()?.RateUsed24K * 22m / 24m:N0}/10g")
                        .FontSize(8);
                    c.Item().Text($"Rate 24K: ₹{bill.Items.FirstOrDefault()?.RateUsed24K:N0}/10g")
                        .FontSize(8);
                    c.Item().Text($"Items: {bill.Items.Count}");
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    // ─── Content: items table + totals ──────────────────────────────────────

    private static void ComposeContent(IContainer container, BillDto bill)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(8).Element(c => ComposeItemsTable(c, bill));
            col.Item().PaddingTop(12).Element(c => ComposeTotals(c, bill));
            col.Item().PaddingTop(12).Element(c => ComposePaymentSection(c, bill));
        });
    }

    private static void ComposeItemsTable(IContainer container, BillDto bill)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(20);   // #
                cols.RelativeColumn(3);    // Name
                cols.RelativeColumn(1.5f); // Purity
                cols.RelativeColumn(1.2f); // Gross
                cols.RelativeColumn(1.2f); // Stone
                cols.RelativeColumn(1.2f); // Net
                cols.RelativeColumn(1.2f); // Wastage
                cols.RelativeColumn(2);    // Making
                cols.RelativeColumn(1.5f); // Gold Val
                cols.RelativeColumn(1.5f); // Total
            });

            // Header row
            table.Header(header =>
            {
                static void HeaderCell(IContainer c, string text)
                    => c.Background(Colors.Orange.Darken3)
                         .Padding(4)
                         .Text(text).Bold().FontSize(8).FontColor(Colors.White);

                header.Cell().Element(c => HeaderCell(c, "#"));
                header.Cell().Element(c => HeaderCell(c, "Item"));
                header.Cell().Element(c => HeaderCell(c, "Purity"));
                header.Cell().Element(c => HeaderCell(c, "Gross\n(g)"));
                header.Cell().Element(c => HeaderCell(c, "Stone\n(g)"));
                header.Cell().Element(c => HeaderCell(c, "Net\n(g)"));
                header.Cell().Element(c => HeaderCell(c, "Wastage\n(g)"));
                header.Cell().Element(c => HeaderCell(c, "Making"));
                header.Cell().Element(c => HeaderCell(c, "Gold\n₹"));
                header.Cell().Element(c => HeaderCell(c, "Total\n₹"));
            });

            // Data rows
            for (int i = 0; i < bill.Items.Count; i++)
            {
                var item = bill.Items[i];
                bool isAlt = i % 2 == 0;
                var bg = isAlt ? Colors.Grey.Lighten4 : Colors.White;

                static IContainer DataCell(IContainer c, bool alt)
                    => c.Background(alt ? Colors.Grey.Lighten4 : Colors.White).Padding(3);

                table.Cell().Element(c => DataCell(c, isAlt)).Text($"{i + 1}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt)).Column(cc =>
                {
                    cc.Item().Text(item.Item.Name).FontSize(8).Bold();
                    cc.Item().Text($"Tag: {item.Item.TagNo}").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                table.Cell().Element(c => DataCell(c, isAlt)).Text(item.Item.Purity).FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt)).Text($"{item.GrossWeight:N3}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt)).Text($"{item.StoneWeight:N3}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt)).Text($"{item.NetWeight:N3}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt)).Text($"{item.WastageWeight:N3}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt))
                    .Text($"₹{item.MakingAmount:N0}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt))
                    .Text($"₹{item.GoldValue:N0}").FontSize(8);
                table.Cell().Element(c => DataCell(c, isAlt))
                    .Text($"₹{item.LineTotal:N0}").Bold().FontSize(8);
            }
        });
    }

    private static void ComposeTotals(IContainer container, BillDto bill)
    {
        container.Row(row =>
        {
            row.RelativeItem(); // left spacer

            row.RelativeItem(2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                });

                static void TotalRow(TableDescriptor t, string label, decimal value,
                    bool bold = false, string? color = null)
                {
                    var lbl = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(2).PaddingHorizontal(4)
                                .Text(label).FontSize(9);
                    if (bold) lbl.Bold();

                    var val = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(2).PaddingHorizontal(4)
                                .AlignRight().Text($"₹{value:N2}").FontSize(9);
                    if (bold) val.Bold();
                }

                TotalRow(table, "Gold Value", bill.GoldValue);
                TotalRow(table, "Making Charges", bill.MakingAmount);
                TotalRow(table, "Wastage", bill.WastageAmount);
                TotalRow(table, "Stone Charges", bill.StoneCharge);
                TotalRow(table, "Sub-Total", bill.GoldValue + bill.MakingAmount + bill.WastageAmount + bill.StoneCharge);
                if (bill.DiscountAmount > 0)
                    TotalRow(table, "Discount (–)", bill.DiscountAmount);

                if (bill.CGST > 0)
                {
                    TotalRow(table, "CGST (1.5%)", bill.CGST);
                    TotalRow(table, "SGST (1.5%)", bill.SGST);
                }

                if (bill.IGST > 0)
                    TotalRow(table, "IGST (3%)", bill.IGST);

                if (bill.RoundOff != 0)
                    TotalRow(table, "Round Off", bill.RoundOff);

                // Grand total highlighted
                table.Cell()
                    .Background(Colors.Orange.Darken3)
                    .Padding(4)
                    .Text("Grand Total").Bold().FontSize(10).FontColor(Colors.White);
                table.Cell()
                    .Background(Colors.Orange.Darken3)
                    .Padding(4)
                    .AlignRight()
                    .Text($"₹{bill.GrandTotal:N2}").Bold().FontSize(10).FontColor(Colors.White);
            });
        });
    }

    private static void ComposePaymentSection(IContainer container, BillDto bill)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Payment Summary").Bold().FontSize(9);
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text($"Mode: {bill.PaymentMode}").FontSize(8);
                        r.RelativeItem().Text($"Grand Total: ₹{bill.GrandTotal:N2}").FontSize(8);
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Exchange Gold: ₹{bill.ExchangeValue:N2}").FontSize(8);
                        r.RelativeItem().Text($"Amount Paid: ₹{bill.AmountPaid:N2}").FontSize(8);
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem();
                        r.RelativeItem().Text($"Balance Due: ₹{bill.BalanceDue:N2}")
                            .Bold().FontSize(9)
                            .FontColor(bill.BalanceDue > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                    });
                });

                row.ConstantItem(120).Column(c =>
                {
                    c.Item().AlignCenter().Text("Customer Signature").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Height(40).Border(0.5f).BorderColor(Colors.Grey.Lighten1);
                    c.Item().PaddingTop(4).AlignCenter()
                        .Text("Authorised Signatory").FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().Height(40).Border(0.5f).BorderColor(Colors.Grey.Lighten1);
                });
            });
        });
    }

    // ─── Footer ──────────────────────────────────────────────────────────────

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem()
                .Text("Thank you for shopping with us! All sales are subject to our standard terms & conditions.")
                .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();

            row.ConstantItem(60).AlignRight()
                .Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }
}
