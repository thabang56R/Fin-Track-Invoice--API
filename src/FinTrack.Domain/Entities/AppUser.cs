using System.Text.Json.Serialization;

namespace FinTrack.Domain.Entities;


public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
