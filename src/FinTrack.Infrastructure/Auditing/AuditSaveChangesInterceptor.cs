using System.Text.Json;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FinTrack.Infrastructure.Auditing;

public interface IUserContext
{
    Guid? UserId { get; }
}

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IUserContext _user;
    public AuditSaveChangesInterceptor(IUserContext user) => _user = user;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        WriteAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAudit(DbContext? ctx)
    {
        if (ctx is null) return;

        var entries = ctx.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog)
            .ToList();

        if (entries.Count == 0) return;

        foreach (var e in entries)
        {
            // OPTIONAL but recommended:
            // if invoice is marked Modified only because a payment/item was added,
            // it creates noisy audits + more chances of concurrency conflicts.
            if (e.Entity is Invoice inv && e.State == EntityState.Modified)
            {
                // If only RowVersion changed, skip.
                // (RowVersion always changes when row is updated.)
                var modifiedProps = e.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList();
                if (modifiedProps.Count == 1 && modifiedProps[0] == "RowVersion")
                    continue;
            }

            var entityType = e.Metadata.ClrType.Name;
            var entityId = TryGetPrimaryKeyValue(e);

            var oldValues = e.State == EntityState.Added ? null : GetScalarValues(e.OriginalValues);
            var newValues = e.State == EntityState.Deleted ? null : GetScalarValues(e.CurrentValues);

            ctx.Set<AuditLog>().Add(new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = e.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                },
                OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
                NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
                PerformedByUserId = _user.UserId,
                PerformedAtUtc = DateTime.UtcNow
            });
        }
    }

    // Only scalars + ignore concurrency token
    private static Dictionary<string, object?> GetScalarValues(PropertyValues values)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var prop in values.Properties)
        {
            // Ignore concurrency token / noisy stuff
            if (prop.Name == "RowVersion") continue;

            var value = values[prop.Name];

            // Keep only scalar-ish values; skip complex/nav-like objects
            if (value is null ||
                value is string ||
                value is Guid ||
                value is bool ||
                value is byte ||
                value is short ||
                value is int ||
                value is long ||
                value is float ||
                value is double ||
                value is decimal ||
                value is DateTime ||
                value is DateOnly ||
                value is TimeOnly)
            {
                dict[prop.Name] = value;
            }
            else
            {
                // For anything else, record a simple string to avoid deep object graphs
                dict[prop.Name] = value.ToString();
            }
        }

        return dict;
    }

    private static string TryGetPrimaryKeyValue(EntityEntry e)
    {
        var pk = e.Metadata.FindPrimaryKey();
        if (pk is null) return "";

        var keyProp = pk.Properties.FirstOrDefault();
        if (keyProp is null) return "";

        var val = e.Property(keyProp.Name).CurrentValue ?? e.Property(keyProp.Name).OriginalValue;
        return val?.ToString() ?? "";
    }
}

