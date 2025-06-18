using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Checkpoints;

namespace Shared.Interfaces.Checkpoints;


public interface ICheckpointService
{
    Task<bool> AddAsync(Checkpoint model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Checkpoint model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Checkpoint?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Checkpoint>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}