using Shared.Models.Dashboards;

namespace Shared.Interfaces.Dashboards;


public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(DateOnly? startDate = null, DateOnly? endDate = null);
    Task<MetricsTrendDto> GetMetricsTrendsAsync(DateOnly? startDate, DateOnly? endDate);
    Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateOnly? startDate = null, DateOnly? endDate = null);
    Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateOnly? startDate = null, DateOnly? endDate = null);
    Task<List<RecentTripDto>> GetRecentTripsAsync(int count = 5, DateOnly? startDate = null, DateOnly? endDate = null);
}