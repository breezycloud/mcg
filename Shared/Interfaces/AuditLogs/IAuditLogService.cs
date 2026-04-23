using System.Text.Json;
using Shared.Helpers;
using Shared.Models.Logging;

namespace Shared.Interfaces.AuditLogs;


public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, string entityId, 
                 Guid userId, string userName, 
                 object oldValues = null, object newValues = null,
                 string ipAddress = null, string additionalInfo = null);
    
    Task<GridDataResponse<AuditLog>> GetPagedAsync(int page, int pageSize,
                                            string searchTerm = null,
                                            DateTime? fromDate = null,
                                            DateTime? toDate = null,
                                            string entityType = null,
                                            string action = null);
}