using Shared.Models.Trucks;

namespace Shared.Interfaces.Trucks;

// startDate = null means "All Time" (from inception, no lower bound) — matches ControlRoomService's
// convention, including classifying trips by their loading month rather than dispatch date.
public interface ITruckReportService
{
    Task<TruckFleetReportMetricsDto> GetMetricsAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default);
    Task<List<TruckStatusBreakdownDto>> GetStatusBreakdownAsync(string? product = "All", CancellationToken cancellationToken = default);
    Task<List<TruckPerformanceRowDto>> GetTruckPerformanceAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default);
    Task<List<MaintenanceSpendByTruckDto>> GetMaintenanceSpendByTruckAsync(DateOnly? startDate = null, int count = 10, CancellationToken cancellationToken = default);
    Task<List<MaintenanceSpendByCategoryDto>> GetMaintenanceSpendByCategoryAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<CalibrationExpiryDto>> GetCalibrationExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default);
    Task<List<FleetMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, string? product = "All", CancellationToken cancellationToken = default);
}
