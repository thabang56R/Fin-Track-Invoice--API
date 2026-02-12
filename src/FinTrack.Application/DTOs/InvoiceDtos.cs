namespace FinTrack.Application.DTOs;

public record CreateCustomerRequest(string Name, string? Email, string? Phone, string? Address);

public record CreateInvoiceItem(string Description, int Qty, decimal UnitPrice, decimal VatRate);

public record CreateInvoiceRequest(
    Guid CustomerId,
    DateOnly IssueDate,
    DateOnly DueDate,
    List<CreateInvoiceItem> Items
);

public record ApplyPaymentRequest(decimal Amount, string Method, string? Reference);
