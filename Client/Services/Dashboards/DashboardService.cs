using System.Net.Http.Json;
using Shared.Models.Dashboards;

namespace Shared.Interfaces.Dashboards;


public class DashboardService(IHttpClientFactory _httpClient) : IDashboardService
{

    public async Task<MetricsTrendDto> GetMetricsTrendsAsync(DateTime? startDate, DateTime? endDate)
    {
        var currentMetrics = await GetMetricsAsync(startDate, endDate);
        
        // Get comparison period (e.g., previous week/month)
        var (prevStart, prevEnd) = GetComparisonPeriod(startDate, endDate);
        var previousMetrics = await GetMetricsAsync(prevStart, prevEnd);

        return new MetricsTrendDto
        {
            TotalTripsTrend = CalculateTrend(currentMetrics.TotalTrips, previousMetrics.TotalTrips),
            ActiveTripsTrend = CalculateTrend(currentMetrics.ActiveTrips, previousMetrics.ActiveTrips),
            CompletedTripsTrend = CalculateTrend(currentMetrics.CompletedTrips, previousMetrics.CompletedTrips),
            AvgDurationTrend = CalculateTrend(currentMetrics.AvgTripDurationDays, previousMetrics.AvgTripDurationDays)
        };
    }

    private (DateTime?, DateTime?) GetComparisonPeriod(DateTime? start, DateTime? end)
    {
        if (!start.HasValue || !end.HasValue) 
            return (null, null);

        var diff = end.Value - start.Value;
        return (start.Value - diff, end.Value - diff);
    }

    private double CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0) return 0;
        return (double)((current - previous) / previous * 100);
    }
    public async Task<DashboardMetricsDto> GetMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
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
    public async Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
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
    public async Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateTime? startDate = null, DateTime? endDate = null)
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
    public async Task<List<RecentTripDto>> GetRecentTripsAsync(int count = 5, DateTime? startDate = null, DateTime? endDate = null)
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