namespace FinTrack.Application.DTOs;

public class ReversePaymentRequest
{
    public string? Reason { get; set; }
    public string Method { get; set; } = "EFT";
    public string? Reference { get; set; }
}
