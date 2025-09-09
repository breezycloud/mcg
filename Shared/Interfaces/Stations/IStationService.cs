using Shared.Helpers;
using Shared.Models.Stations;

namespace Shared.Interfaces.Stations;

public interface IStationService
{
    Task<bool> AddAsync(Station model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Station model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Station?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Station[]?> GetAsync(CancellationToken cancellationToken);
    Task<Station[]?> GetAsync(string type, CancellationToken cancellationToken);
    Task<Station[]?> GetAsync(string type, string state, CancellationToken cancellationToken);
    Task<GridDataResponse<Station>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    Task<GridDataResponse<Station>?> GetPagedAsync(string type, GridDataRequest request, CancellationToken cancellationToken);
}