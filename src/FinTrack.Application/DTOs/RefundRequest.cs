namespace FinTrack.Application.DTOs;

public class RefundRequest
{
    public decimal Amount { get; set; }  
    public string? Reason { get; set; }
    public string Method { get; set; } = "EFT";
    public string? Reference { get; set; }
}
