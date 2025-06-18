using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.RefuelInfos;

namespace Shared.Interfaces.RefuelInfos;


public interface IRefuelInfoService
{
    Task<bool> AddAsync(RefuelInfo model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(RefuelInfo model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<RefuelInfo?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<RefuelInfo>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}