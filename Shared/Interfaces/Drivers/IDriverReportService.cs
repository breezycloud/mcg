using Shared.Models.Drivers;

namespace Shared.Interfaces.Drivers;

// startDate = null means "All Time" (from inception, no lower bound) — matches
// ITruckReportService's convention.
public interface IDriverReportService
{
    Task<DriverFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<DriverPerformanceRowDto>> GetDriverPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<DriverLicenseExpiryDto>> GetLicenseExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default);
    Task<List<DriverMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default);
}
