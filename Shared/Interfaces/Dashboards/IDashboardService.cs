using Shared.Models.Dashboards;

namespace Shared.Interfaces.Dashboards;


public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<MetricsTrendDto> GetMetricsTrendsAsync(DateTime? startDate, DateTime? endDate);
    Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<RecentTripDto>> GetRecentTripsAsync(int count = 5, DateTime? startDate = null, DateTime? endDate = null);
}