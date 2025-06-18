using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Destinations;


public interface IDestinationService
{
    Task<bool> AddAsync(Destination model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Destination model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Destination?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Destination>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}