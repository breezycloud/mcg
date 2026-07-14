using Shared.Models.Stations;

namespace Shared.Interfaces.Stations;

// startDate = null means "All Time" (from inception, no lower bound) — matches
// ITruckReportService's convention.
public interface IStationReportService
{
    Task<StationFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<StationPerformanceRowDto>> GetStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<StationMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default);

    Task<LoadingDepotFleetMetricsDto> GetLoadingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<LoadingDepotPerformanceRowDto>> GetLoadingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<LoadingDepotMonthlyTrendDto>> GetLoadingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default);

    Task<ReceivingDepotFleetMetricsDto> GetReceivingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<ReceivingDepotPerformanceRowDto>> GetReceivingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<ReceivingDepotMonthlyTrendDto>> GetReceivingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default);

    Task<RefuellingStationFleetMetricsDto> GetRefuellingStationMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<RefuellingStationPerformanceRowDto>> GetRefuellingStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<RefuellingStationMonthlyTrendDto>> GetRefuellingStationMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default);
}
