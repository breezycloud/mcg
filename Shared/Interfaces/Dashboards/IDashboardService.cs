using Shared.Models.Dashboards;

namespace Shared.Interfaces.Dashboards;


public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(DateOnly? startDate = null, DateOnly? endDate = null, string? product = "All");
    Task<MetricsTrendDto> GetMetricsTrendsAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All");
    Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateOnly? startDate = null, DateOnly? endDate = null, string? product = "All");
    Task<List<TripMonthlySummaryDto>> GetTripMonthlySummaries(string? product = "All");
    Task<List<TripMonthlyProductSummary>> GetTripMonthlyProductSummaries(string? product = "All");
    Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateOnly? startDate = null, DateOnly? endDate = null, string? product = "All");
    Task<List<RecentTripDto>> GetRecentTripsAsync(int count = 5, DateOnly? startDate = null, DateOnly? endDate = null, string? product = "All");
}