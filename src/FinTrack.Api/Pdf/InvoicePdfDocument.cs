using FinTrack.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinTrack.Api.Pdf;

public class InvoicePdfDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly string _companyName;

    public InvoicePdfDocument(Invoice invoice, string companyName = "FinTrack")
    {
        _invoice = invoice;
        _companyName = companyName;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.ConstantItem(80).Image("wwwroot/logo.png");

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_companyName).FontSize(18).SemiBold();
                col.Item().Text("Invoice & Payment Management").FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(200).AlignRight().Column(col =>
            {
                col.Item().Text($"INVOICE: {_invoice.InvoiceNumber}").SemiBold();
                col.Item().Text($"Status: {_invoice.Status}");
                col.Item().Text($"Issue Date: {_invoice.IssueDate:yyyy-MM-dd}");
                col.Item().Text($"Due Date: {_invoice.DueDate:yyyy-MM-dd}");
            });
        });

        container.PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(15);

            col.Item().Element(ComposeCustomerSection);
            col.Item().Element(ComposeItemsTable);
            col.Item().Element(ComposeVatBreakdown);
            col.Item().AlignRight().Element(ComposeTotals);
            col.Item().Element(ComposePaymentsSection);
        });
    }

    private void ComposeCustomerSection(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Item().Text("Bill To").SemiBold();
            col.Item().Text(_invoice.Customer?.Name ?? "Unknown").SemiBold();
            col.Item().Text(_invoice.Customer?.Email ?? "");
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Text("Items").SemiBold();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Text("Description").SemiBold();
                header.Cell().AlignRight().Text("Qty").SemiBold();
                header.Cell().AlignRight().Text("Unit Price").SemiBold();
                header.Cell().AlignRight().Text("Line Total").SemiBold();
            });

            foreach (var item in _invoice.Items)
            {
                table.Cell().Text(item.Description);
                table.Cell().AlignRight().Text(item.Qty.ToString());
                table.Cell().AlignRight().Text(Money(item.UnitPrice));
                table.Cell().AlignRight().Text(Money(item.LineTotal));
            }
        });
    }

    private void ComposeVatBreakdown(IContainer container)
    {
        var vatGroups = _invoice.Items
            .GroupBy(i => i.VatRate)
            .Select(g => new
            {
                VatRate = g.Key,
                VatAmount = g.Sum(x => x.VatAmount)
            });

        container.Column(col =>
        {
            col.Item().Text("VAT Breakdown").SemiBold();

            foreach (var vat in vatGroups)
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"VAT @ {vat.VatRate:P0}");
                    row.ConstantItem(120).AlignRight().Text(Money(vat.VatAmount));
                });
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        var paid = _invoice.Payments.Sum(p => p.Amount);
        var outstanding = _invoice.Total - paid;

        container.Width(250).Border(1).Padding(10).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Subtotal");
                r.ConstantItem(100).AlignRight().Text(Money(_invoice.Subtotal));
            });

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("VAT");
                r.ConstantItem(100).AlignRight().Text(Money(_invoice.VatTotal));
            });

            col.Item().LineHorizontal(1);

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Total").SemiBold();
                r.ConstantItem(100).AlignRight().Text(Money(_invoice.Total)).SemiBold();
            });

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Paid");
                r.ConstantItem(100).AlignRight().Text(Money(paid));
            });

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Outstanding").SemiBold();
                r.ConstantItem(100).AlignRight().Text(Money(outstanding)).SemiBold();
            });
        });
    }

    private void ComposePaymentsSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Payment History").SemiBold();

            if (!_invoice.Payments.Any())
            {
                col.Item().Text("No payments recorded.");
                return;
            }

            foreach (var p in _invoice.Payments.OrderBy(x => x.CapturedAtUtc))
            {
                var label = p.Amount < 0
                    ? "Refund / Reversal"
                    : "Payment";

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"{label} ({p.Method})");
                    row.ConstantItem(120).AlignRight().Text(Money(p.Amount));
                });

                if (!string.IsNullOrWhiteSpace(p.Reference))
                    col.Item().Text($"Ref: {p.Reference}").FontColor(Colors.Grey.Darken1);

                if (!string.IsNullOrWhiteSpace(p.Reason))
                    col.Item().Text($"Reason: {p.Reason}").FontColor(Colors.Grey.Darken1);

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            }
        });
    }

    private static string Money(decimal value) => value.ToString("0.00");
}
