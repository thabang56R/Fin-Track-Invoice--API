using System.Text.Json.Serialization;

namespace FinTrack.Domain.Entities;


public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    [JsonIgnore]
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "EFT";
    public string? Reference { get; set; }

    public Guid CapturedByUserId { get; set; }

    public Guid? ReversedPaymentId { get; set; }   
    public string? Reason { get; set; }            

}
