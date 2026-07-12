using System.Net.Http.Json;
using Shared.Interfaces.Drivers;
using Shared.Models.Drivers;

namespace Client.Services.Drivers;

public class DriverReportService(IHttpClientFactory _httpClient) : IDriverReportService
{
    public async Task<DriverFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"driverreport/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DriverFleetMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<DriverPerformanceRowDto>> GetDriverPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"driverreport/performance?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DriverPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<DriverLicenseExpiryDto>> GetLicenseExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"driverreport/license-expiry?withinDays={withinDays}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DriverLicenseExpiryDto>>(cancellationToken) ?? [];
    }

    public async Task<List<DriverMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"driverreport/monthly-trend?months={months}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DriverMonthlyTrendDto>>(cancellationToken) ?? [];
    }
}
