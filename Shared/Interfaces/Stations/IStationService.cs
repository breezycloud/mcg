using Shared.Helpers;
using Shared.Models.Stations;

namespace Shared.Interfaces.Stations;

public interface IStationService
{
     Task<bool> AddAsync(Station model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Station model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Station?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Station>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}