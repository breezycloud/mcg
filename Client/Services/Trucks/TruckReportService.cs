using System.Net.Http.Json;
using Shared.Interfaces.Trucks;
using Shared.Models.Trucks;

namespace Client.Services.Trucks;

public class TruckReportService(IHttpClientFactory _httpClient) : ITruckReportService
{
    public async Task<TruckFleetReportMetricsDto> GetMetricsAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/metrics?startDate={startDate}&product={product}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TruckFleetReportMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<TruckStatusBreakdownDto>> GetStatusBreakdownAsync(string? product = "All", CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/status-breakdown?product={product}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TruckStatusBreakdownDto>>(cancellationToken) ?? [];
    }

    public async Task<List<TruckPerformanceRowDto>> GetTruckPerformanceAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/performance?startDate={startDate}&product={product}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TruckPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<MaintenanceSpendByTruckDto>> GetMaintenanceSpendByTruckAsync(DateOnly? startDate = null, int count = 10, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/maintenance-spend/by-truck?startDate={startDate}&count={count}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MaintenanceSpendByTruckDto>>(cancellationToken) ?? [];
    }

    public async Task<List<MaintenanceSpendByCategoryDto>> GetMaintenanceSpendByCategoryAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/maintenance-spend/by-category?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MaintenanceSpendByCategoryDto>>(cancellationToken) ?? [];
    }

    public async Task<List<CalibrationExpiryDto>> GetCalibrationExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/calibration-expiry?withinDays={withinDays}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CalibrationExpiryDto>>(cancellationToken) ?? [];
    }

    public async Task<List<FleetMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, string? product = "All", CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"truckreport/monthly-trend?months={months}&product={product}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<FleetMonthlyTrendDto>>(cancellationToken) ?? [];
    }
}
