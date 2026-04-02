using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Models.Logging;

namespace Api.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically writes an AuditLog row for every
/// Create / Update / Delete operation across all tracked entities.
/// Excluded: AuditLog itself (to prevent recursion) and LogMessage (application error logs).
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> ExcludedTypes =
    [
        typeof(AuditLog),
        typeof(LogMessage)
    ];

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Called synchronously inside SaveChanges
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            CollectAndAddAuditEntries(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    // Called inside SaveChangesAsync
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CollectAndAddAuditEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectAndAddAuditEntries(DbContext context)
    {
        var userId   = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var timestamp = DateTimeOffset.UtcNow;

        // Snapshot the relevant entries BEFORE we touch the change tracker.
        var entries = context.ChangeTracker.Entries()
            .Where(e =>
                !ExcludedTypes.Contains(e.Entity.GetType()) &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return;

        var auditLogs = new List<AuditLog>(entries.Count);

        foreach (var entry in entries)
        {
            string action = entry.State switch
            {
                EntityState.Added    => "Create",
                EntityState.Modified => "Update",
                _                    => "Delete"
            };

            // Build a human-readable entity ID from the primary key.
            var keyProps = entry.Metadata.FindPrimaryKey()?.Properties;
            var entityId = keyProps is not null
                ? string.Join(",", keyProps.Select(p => entry.Property(p.Name).CurrentValue?.ToString()))
                : null;

            string? oldValues    = null;
            string? newValues    = null;
            string? affectedFields = null;

            if (entry.State == EntityState.Modified)
            {
                var changed = entry.Properties.Where(p => p.IsModified).ToList();
                if (changed.Count == 0) continue; // nothing actually changed

                oldValues = SerializeDict(changed.ToDictionary(
                    p => p.Metadata.Name,
                    p => p.OriginalValue));

                newValues = SerializeDict(changed.ToDictionary(
                    p => p.Metadata.Name,
                    p => p.CurrentValue));

                affectedFields = string.Join(", ", changed.Select(p => p.Metadata.Name));
            }
            else if (entry.State == EntityState.Added)
            {
                newValues = SerializeDict(entry.Properties.ToDictionary(
                    p => p.Metadata.Name,
                    p => p.CurrentValue));
            }
            else // Deleted
            {
                oldValues = SerializeDict(entry.Properties.ToDictionary(
                    p => p.Metadata.Name,
                    p => p.CurrentValue));
            }

            auditLogs.Add(new AuditLog
            {
                Id             = Guid.NewGuid(),
                Action         = action,
                EntityType     = entry.Entity.GetType().Name,
                EntityId       = entityId,
                UserId         = userId,
                UserName       = userName,
                OldValues      = oldValues,
                NewValues      = newValues,
                AffectedFields = affectedFields,
                Timestamp      = timestamp,
                IpAddress      = ipAddress
            });
        }

        context.Set<AuditLog>().AddRange(auditLogs);
    }

    /// <summary>
    /// Serializes a property-value dictionary to JSON.
    /// Falls back to string conversion if full serialization fails.
    /// </summary>
    private static string? SerializeDict(Dictionary<string, object?> dict)
    {
        if (dict.Count == 0) return null;
        try
        {
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            // Fallback: stringify every value to guarantee we can still write the row.
            return JsonSerializer.Serialize(
                dict.ToDictionary(k => k.Key, k => (object?)k.Value?.ToString()));
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? "System";
    }
}
