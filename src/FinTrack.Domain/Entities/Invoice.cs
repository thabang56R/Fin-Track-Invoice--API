using FinTrack.Domain.Enums;

using System.Text.Json.Serialization;

namespace FinTrack.Domain.Entities;


public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    [JsonIgnore]
    public Customer? Customer { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal Total { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<InvoiceItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();

    public byte[]? RowVersion { get; set; }


    public decimal PaidAmount => Payments.Sum(p => p.Amount);
    public decimal Outstanding => Total - PaidAmount;
}
