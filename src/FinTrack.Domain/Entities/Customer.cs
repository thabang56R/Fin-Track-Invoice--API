using System.Text.Json.Serialization;   

namespace FinTrack.Domain.Entities;


public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
