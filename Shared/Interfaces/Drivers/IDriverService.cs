using Shared.Helpers;
using Shared.Models.Drivers;

namespace Shared.Interfaces.Drivers;


public interface IDriverService
{
    Task<bool> AddAsync(Driver model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Driver model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Driver?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Driver[]?> GetAsync(CancellationToken cancellationToken);
    Task<GridDataResponse<Driver>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}