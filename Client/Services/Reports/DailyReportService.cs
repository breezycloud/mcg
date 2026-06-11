using System.Net.Http.Json;
using Microsoft.JSInterop;
using Shared.Helpers;
using Shared.Models.Reports;

namespace Shared.Interfaces.Reports;

public class DailyReportService(IHttpClientFactory _httpClient, IJSRuntime _js) : IDailyReportService
{
    public async Task<GridDataResponse<DailyReport>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PostAsJsonAsync("daily-reports/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<DailyReport>?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    public async Task<DailyReport?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .GetAsync($"daily-reports/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DailyReport?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    /// <summary>Creates a report and returns the full persisted entity (with server-assigned Id).</summary>
    public async Task<DailyReport?> AddAsync(DailyReport model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PostAsJsonAsync("daily-reports", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            // 201 Created — body contains the saved entity with server-assigned Id
            return await response.Content.ReadFromJsonAsync<DailyReport?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> UpdateAsync(DailyReport model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PutAsJsonAsync($"daily-reports/{model.Id}", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            return response.IsSuccessStatusCode;
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> UpdateTasksAsync(Guid id, DailyReport model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PatchAsJsonAsync($"daily-reports/{id}/tasks", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            return true;
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .DeleteAsync($"daily-reports/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PutAsJsonAsync($"daily-reports/{id}/submit", new { }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            return response.IsSuccessStatusCode;
        }
        catch (Exception) { throw; }
    }

    public async Task<string?> GenerateReportNoAsync(DateOnly date, Guid? employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"daily-reports/generate-no?date={date:yyyy-MM-dd}";
            if (employeeId.HasValue)
                url += $"&employeeId={employeeId.Value}";

            using var response = await _httpClient.CreateClient("AppUrl").GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<string?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    public async ValueTask ExportToCsvAsync(DailyReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PostAsJsonAsync("daily-reports/report", filter, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var fileName = $"Daily_Report_{filter.StartDate:MMMM-yyyy}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            await _js.InvokeVoidAsync("downloadReport", fileName, Convert.ToBase64String(content));
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> ReviewAsync(Guid id, string comment, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PutAsJsonAsync($"daily-reports/{id}/review", comment, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            return true;
        }
        catch (Exception) { throw; }
    }

    public async Task<bool> RevertAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .PutAsJsonAsync($"daily-reports/{id}/revert", new { }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ReadErrorMessageAsync(response, cancellationToken));
            return true;
        }
        catch (Exception) { throw; }
    }

    public async Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .GetAsync("daily-reports/pending-review-count", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden
             || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return 0; // Non-manager roles get 0 — silently
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
        }
        catch (Exception) { return 0; } // Badge failure is non-critical — never break the UI
    }

    public async Task<DailyReportSummary?> GetSummaryAsync(DateOnly month, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .GetAsync($"daily-reports/summary?month={month:yyyy-MM-dd}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DailyReportSummary?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    public async Task<DailyReport?> GetLatestForCopyAsync(Guid employeeId, DateOnly beforeDate, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl")
                .GetAsync($"daily-reports/latest-for-copy?employeeId={employeeId}&beforeDate={beforeDate:yyyy-MM-dd}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null; // No previous report found — not an error
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DailyReport?>(cancellationToken);
        }
        catch (Exception) { throw; }
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content)) return "Operation failed.";
        return content.Trim().Trim('"');
    }
}
