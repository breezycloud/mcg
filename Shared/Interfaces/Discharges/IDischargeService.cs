using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Discharges;


public interface IDischargeService
{
    Task<bool> AddAsync(Discharge model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Discharge model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Discharge?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Discharge>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}