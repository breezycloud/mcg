using Shared.Helpers;
using Shared.Models.Shops;

namespace Shared.Interfaces.Shops;

public interface IMaintenanceService
{
    Task<bool> AddAsync(MaintenanceSite model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(MaintenanceSite model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<MaintenanceSite?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<MaintenanceSite[]?> GetAsync(CancellationToken cancellationToken);

    Task<GridDataResponse<MaintenanceSite>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}