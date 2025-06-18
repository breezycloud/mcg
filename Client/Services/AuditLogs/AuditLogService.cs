using System.Net.Http.Json;
using System.Text.Json;
using Shared.Helpers;
using Shared.Interfaces.AuditLogs;
using Shared.Models.Logging;

namespace Client.Services.AuditLogs;

public class AuditLogService(IHttpClientFactory _httpClient) : IAuditLogService
{
    public async Task<GridDataResponse<AuditLog>> GetPagedAsync(int page, int pageSize, string searchTerm = null, DateTime? fromDate = null, DateTime? toDate = null, string entityType = null)
    {
        try
        {
            var request = new GridDataRequest() { Page = page, PageSize = pageSize, SearchTerm = searchTerm };
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("audits/paged", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<AuditLog>?>() ?? new GridDataResponse<AuditLog>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public Task LogAsync(string action, string entityType, string entityId, Guid userId, string userName, object oldValues = null, object newValues = null, string ipAddress = null, string additionalInfo = null)
    {
        throw new NotImplementedException();
    }
}