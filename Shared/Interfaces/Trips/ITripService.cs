using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Trips;


public interface ITripService
{
    Task<bool> AddAsync(Trip model, CancellationToken cancellationToken);
    Trip MapTripLoadingAsync(TripLoadingDto model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Trip model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Trip>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}