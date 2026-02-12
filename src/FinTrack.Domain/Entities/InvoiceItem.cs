using System.Text.Json.Serialization;

namespace FinTrack.Domain.Entities;


public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    [JsonIgnore]
    public Invoice? Invoice { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }

    public decimal LineTotal { get; set; }
    public decimal VatAmount { get; set; }
}
