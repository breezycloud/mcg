using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.TripCheckpoints;

namespace Shared.Interfaces.TripCheckpoints;


public interface ITripCheckpointService
{
    Task<bool> AddAsync(TripCheckpoint model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(TripCheckpoint model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<TripCheckpoint?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<TripCheckpoint>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}