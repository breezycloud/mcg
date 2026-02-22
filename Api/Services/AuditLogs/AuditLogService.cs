using System.Text.Json;
using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Models.Logging;

namespace Shared.Interfaces.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, string entityId, 
                             Guid userId, string userName,
                             object oldValues = null, object newValues = null,
                             string ipAddress = null, string additionalInfo = null)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserName = userName,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress ?? _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            AdditionalInfo = additionalInfo,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (oldValues != null && newValues != null)
        {
            var oldDict = JsonSerializer.Deserialize<Dictionary<string, object>>(log.OldValues);
            var newDict = JsonSerializer.Deserialize<Dictionary<string, object>>(log.NewValues);
            log.AffectedFields = string.Join(", ", oldDict.Keys.Where(k => !object.Equals(oldDict[k], newDict[k])));
        }

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<GridDataResponse<AuditLog>> GetPagedAsync(int page, int pageSize,
                                                        string searchTerm = null,
                                                        DateTime? fromDate = null,
                                                        DateTime? toDate = null,
                                                        string entityType = null,
                                                        string action = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(x => 
                x.UserName.Contains(searchTerm) ||
                x.EntityType.Contains(searchTerm) ||
                x.Action.Contains(searchTerm) ||
                x.AdditionalInfo.Contains(searchTerm));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.Timestamp <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(x => x.EntityType == entityType);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new GridDataResponse<AuditLog>
        {
            Data = items,
            Total = totalItems            
        };
    }
}