using System.Net.Http.Json;
using Shared.Interfaces.Stations;
using Shared.Models.Stations;

namespace Client.Services.Stations;

public class StationReportService(IHttpClientFactory _httpClient) : IStationReportService
{
    public async Task<StationFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StationFleetMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<StationPerformanceRowDto>> GetStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/performance?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StationPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<StationMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/monthly-trend?months={months}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<StationMonthlyTrendDto>>(cancellationToken) ?? [];
    }

    public async Task<LoadingDepotFleetMetricsDto> GetLoadingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/loading-depot/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoadingDepotFleetMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<LoadingDepotPerformanceRowDto>> GetLoadingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/loading-depot/performance?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<LoadingDepotPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<LoadingDepotMonthlyTrendDto>> GetLoadingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/loading-depot/monthly-trend?months={months}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<LoadingDepotMonthlyTrendDto>>(cancellationToken) ?? [];
    }

    public async Task<ReceivingDepotFleetMetricsDto> GetReceivingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/receiving-depot/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReceivingDepotFleetMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<ReceivingDepotPerformanceRowDto>> GetReceivingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/receiving-depot/performance?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReceivingDepotPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<ReceivingDepotMonthlyTrendDto>> GetReceivingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/receiving-depot/monthly-trend?months={months}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReceivingDepotMonthlyTrendDto>>(cancellationToken) ?? [];
    }

    public async Task<RefuellingStationFleetMetricsDto> GetRefuellingStationMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/refuelling-station/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RefuellingStationFleetMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<RefuellingStationPerformanceRowDto>> GetRefuellingStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/refuelling-station/performance?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RefuellingStationPerformanceRowDto>>(cancellationToken) ?? [];
    }

    public async Task<List<RefuellingStationMonthlyTrendDto>> GetRefuellingStationMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"stationreport/refuelling-station/monthly-trend?months={months}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RefuellingStationMonthlyTrendDto>>(cancellationToken) ?? [];
    }
}
