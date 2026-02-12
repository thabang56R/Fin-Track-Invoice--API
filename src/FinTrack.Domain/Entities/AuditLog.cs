using System.Text.Json.Serialization;

namespace FinTrack.Domain.Entities;


public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    public Guid? PerformedByUserId { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
}
