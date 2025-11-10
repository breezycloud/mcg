using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Trips;


public interface ITripService
{
    Task<bool> AddAsync(Trip model, CancellationToken cancellationToken);
    Task<Trip?> GetActiveTripForTruckAsync(Guid truckId, CancellationToken cancellationToken);
    Task<List<Trip>> GetTripsByTruckAsync(Guid truckId, int year, CancellationToken cancellationToken = default);
    Trip MapTripLoadingAsync(TripLoadingDto model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Trip model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken);
    ValueTask ExportToCsvAsync(ReportFilter request, CancellationToken cancellationToken);
    ValueTask ExportLoadingInfoCsvAsync(ReportFilter request, CancellationToken cancellationToken);
    Task<GridDataResponse<Trip>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    Task<Trip[]> GetTripsByDateRangeAsync(ReportFilter filter, CancellationToken cancellationToken);
    Task<string?> GenerateDispatchIdAsync(Guid truckId, DateOnly date, CancellationToken cancellationToken);
    Task<bool> DispatchExistAsync(Guid truckId, DateOnly date, CancellationToken cancellationToken);
}