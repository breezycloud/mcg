using System.Net.Http.Json;
using Shared.Enums;
using Shared.Interfaces.Dashboards;
using Shared.Models.Dashboards;

namespace Client.Services.Dashboards;


public class DashboardService(IHttpClientFactory _httpClient) : IDashboardService
{
    
    public async Task<MetricsTrendDto> GetMetricsTrendsAsync(DateOnly? startDate, DateOnly? endDate)
    {
        
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"dashboard/metrics/trends?startDate={startDate!.Value}&endDate={endDate!.Value}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MetricsTrendDto>() ?? new();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"dashboard/metrics?startDate={startDate}&endDate={endDate}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DashboardMetricsDto>() ?? new();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"dashboard/status-distribution?startDate={startDate}&endDate={endDate}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TripStatusDistributionDto>() ?? new();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"dashboard/product-shipments?startDate={startDate}&endDate={endDate}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ProductShipmentDto>>() ?? [];
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<List<RecentTripDto>> GetRecentTripsAsync(int count = 5, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"dashboard/recent-trips?startDate={startDate}&endDate={endDate}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<RecentTripDto>>() ?? [];
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}